using AutoMapper;
using HotelPOS.Application.DTOs.User;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>User accounts — requires a valid JWT token on all endpoints; most actions are Admin-only.</summary>
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserService _userService;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IUserContext userContext, IMapper mapper)
        {
            _userService = userService;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(_mapper.Map<IEnumerable<UserDto>>(users));
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] AddUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and password are required.");

            var (success, error) = await _userService.AddUserAsync(request.Username, request.Password, request.Role, request.RoleId);
            if (!success) return BadRequest(error);

            return NoContent();
        }

        [HttpPost("{id:int}/toggle-active")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> ToggleActive(int id, [FromBody] ToggleActiveRequest request)
        {
            if (id <= 0) return BadRequest("Invalid user ID.");
            await _userService.ToggleActiveAsync(id, request.IsActive);
            return NoContent();
        }

        [HttpPost("{id:int}/set-email")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> SetEmail(int id, [FromBody] SetEmailRequest request)
        {
            if (id <= 0) return BadRequest("Invalid user ID.");
            await _userService.SetEmailAsync(id, request.Email);
            return NoContent();
        }

        // Self-service: a user may reset their own password; EnsureSelfOrPermission in the
        // service layer allows this for the authenticated user's own ID, Admin otherwise.
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            if (id <= 0) return BadRequest("Invalid user ID.");
            if (string.IsNullOrWhiteSpace(request.NewPassword)) return BadRequest("A new password is required.");

            var (success, error) = await _userService.ResetPasswordAsync(id, request.NewPassword);
            if (!success) return BadRequest(error);

            return NoContent();
        }

        // Self-service: a user may enroll/disable 2FA on their own account (see ResetPassword note above).
        [HttpPost("{id:int}/two-factor")]
        public async Task<IActionResult> SetTwoFactor(int id, [FromBody] SetTwoFactorRequest request)
        {
            if (id <= 0) return BadRequest("Invalid user ID.");
            await _userService.SetTwoFactorAsync(id, request.Enabled, request.Secret);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0) return BadRequest("Invalid user ID.");
            var currentUserId = _userContext.CurrentUserId ?? 0;

            try
            {
                await _userService.DeleteUserAsync(id, currentUserId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }
    }

    public sealed class AddUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = RoleNames.Cashier;
        public int RoleId { get; set; }
    }

    public sealed class ToggleActiveRequest
    {
        public bool IsActive { get; set; }
    }

    public sealed class SetEmailRequest
    {
        public string? Email { get; set; }
    }

    public sealed class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public sealed class SetTwoFactorRequest
    {
        public bool Enabled { get; set; }
        public string? Secret { get; set; }
    }
}
