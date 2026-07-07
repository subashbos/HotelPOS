using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Application.UseCases
{
    public class RememberMeService : IRememberMeService
    {
        private const int TokenDays = 30;

        private readonly IUserRepository _userRepository;
        private readonly IRememberMeTokenRepository _tokenRepository;

        public RememberMeService(IUserRepository userRepository, IRememberMeTokenRepository tokenRepository)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
        }

        public async Task<string> IssueTokenAsync(int userId)
        {
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            await _tokenRepository.AddAsync(new RememberMeToken
            {
                UserId = userId,
                TokenHash = HashToken(rawToken),
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(TokenDays)
            });

            return rawToken;
        }

        public async Task<User?> ValidateAndConsumeAsync(string username, string rawToken)
        {
            if (string.IsNullOrEmpty(rawToken)) return null;

            var existing = await _tokenRepository.GetByHashAsync(HashToken(rawToken));
            if (existing == null || existing.RevokedUtc != null || existing.ExpiresUtc <= DateTime.UtcNow)
            {
                return null;
            }

            var user = await _userRepository.GetByIdAsync(existing.UserId);

            // Single-use: revoke regardless of outcome so a stolen/replayed token can't be reused.
            existing.RevokedUtc = DateTime.UtcNow;
            await _tokenRepository.UpdateAsync(existing);

            if (user == null || !user.IsActive || user.MustChangePassword || user.TwoFactorEnabled ||
                !string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                // 2FA-enabled accounts must always complete the full challenge; never silently auto-login them.
                return null;
            }

            return user;
        }

        public async Task RevokeAsync(string rawToken)
        {
            if (string.IsNullOrEmpty(rawToken)) return;

            var existing = await _tokenRepository.GetByHashAsync(HashToken(rawToken));
            if (existing != null && existing.RevokedUtc == null)
            {
                existing.RevokedUtc = DateTime.UtcNow;
                await _tokenRepository.UpdateAsync(existing);
            }
        }

        private static string HashToken(string token) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
