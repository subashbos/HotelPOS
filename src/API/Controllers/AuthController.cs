using HotelPOS.Api.Configuration;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Api.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtOptions _jwtOptions;

        private const int RefreshTokenDays = 30;

        public AuthController(
            IAuthService authService,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<JwtOptions> jwtOptions)
        {
            _authService = authService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtOptions = jwtOptions.Value;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var dto = loginDto ?? new LoginDto();
            var user = await _authService.AuthenticateAsync(dto.Username ?? string.Empty, dto.Password ?? string.Empty);
            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid username or password, or the account is locked/inactive." });
            }

            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(dto.TotpCode))
                {
                    return Unauthorized(new
                    {
                        Message = "Two-factor authentication code required.",
                        RequiresTwoFactor = true
                    });
                }

                if (!TotpGenerator.ValidateCode(user.TwoFactorSecret, dto.TotpCode))
                {
                    return Unauthorized(new { Message = "Invalid authentication code." });
                }
            }

            if (user.MustChangePassword)
            {
                return Unauthorized(new
                {
                    Message = "Password change required before accessing the API.",
                    MustChangePassword = true
                });
            }

            var accessToken = GenerateJwtToken(user);
            var refreshToken = await IssueRefreshTokenAsync(user.Id);

            return Ok(new
            {
                Token = accessToken,
                RefreshToken = refreshToken,
                Username = user.Username,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            var providedHash = HashToken(dto?.RefreshToken ?? string.Empty);
            var existing = await _refreshTokenRepository.GetByHashAsync(providedHash);

            if (existing == null || existing.RevokedUtc != null || existing.ExpiresUtc <= DateTime.UtcNow)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token." });
            }

            var user = await _userRepository.GetByIdAsync(existing.UserId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { Message = "Account is no longer active." });
            }

            // Rotate: issue a new refresh token and revoke the one just used.
            var newRefreshToken = await IssueRefreshTokenAsync(user.Id);
            existing.RevokedUtc = DateTime.UtcNow;
            existing.ReplacedByTokenHash = HashToken(newRefreshToken);
            await _refreshTokenRepository.UpdateAsync(existing);

            var accessToken = GenerateJwtToken(user);

            return Ok(new
            {
                Token = accessToken,
                RefreshToken = newRefreshToken,
                Username = user.Username,
                Role = user.Role,
                MustChangePassword = user.MustChangePassword
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto)
        {
            if (!string.IsNullOrEmpty(dto?.RefreshToken))
            {
                var hash = HashToken(dto.RefreshToken);
                var existing = await _refreshTokenRepository.GetByHashAsync(hash);
                if (existing != null && existing.RevokedUtc == null)
                {
                    existing.RevokedUtc = DateTime.UtcNow;
                    await _refreshTokenRepository.UpdateAsync(existing);
                }
            }

            return Ok();
        }

        private async Task<string> IssueRefreshTokenAsync(int userId)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(rawToken),
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(RefreshTokenDays)
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            return rawToken;
        }

        private static string HashToken(string token)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _jwtOptions.Key ?? throw new InvalidOperationException("JWT Key not configured.");

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
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>Required only when the account has two-factor authentication enabled.</summary>
        public string? TotpCode { get; set; }
    }

    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
