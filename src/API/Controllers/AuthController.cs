using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Api.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return BadRequest(new { Message = "Request body is required." });
            }

            var username = loginDto.Username ?? string.Empty;
            var password = loginDto.Password ?? string.Empty;

            var user = await _authService.AuthenticateAsync(username, password);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid username or password, or the account is locked/inactive." });
            }

            if (user.MustChangePassword)
            {
                return Unauthorized(new
                {
                    Message = "Password change required before accessing the API.",
                    MustChangePassword = true
                });
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured.");
            var jwtIssuer = _config["Jwt:Issuer"] ?? "HotelPOS";
            var jwtAudience = _config["Jwt:Audience"] ?? "HotelPOSClient";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("mustChangePassword", user.MustChangePassword.ToString().ToLower())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
