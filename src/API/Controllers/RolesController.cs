using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Roles and their module permissions — requires a valid JWT token on all endpoints.</summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class RolesController : BaseApiController
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();
            return Ok(roles.Select(ToDto));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            if (id <= 0) return BadRequest("Invalid role ID.");
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(ToDto(role));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Role name is required.");

            var created = await _roleService.AddRoleAsync(request.Name, request.Description ?? string.Empty);
            if (!created) return Conflict($"A role named '{request.Name}' already exists.");

            return NoContent();
        }

        [HttpPut("{id:int}/permissions")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] List<RolePermission> permissions)
        {
            if (id <= 0) return BadRequest("Invalid role ID.");

            try
            {
                await _roleService.UpdateRolePermissionsAsync(id, permissions);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            if (id <= 0) return BadRequest("Invalid role ID.");

            try
            {
                await _roleService.DeleteRoleAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static RoleDto ToDto(Role role) => new()
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.Permissions
        };
    }

    public sealed class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RolePermission> Permissions { get; set; } = new();
    }

    public sealed class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
