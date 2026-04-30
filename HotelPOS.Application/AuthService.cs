using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace HotelPOS.Application
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        // PBKDF2 Configurations
        private const int Iterations = 100000;
        private const int KeySize = 32; // 256 bits
        private const int SaltSize = 16; // 128 bits
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<string, FailedLoginState> FailedLogins =
            new(StringComparer.OrdinalIgnoreCase);

        private sealed class FailedLoginState
        {
            public int Attempts { get; set; }
            public DateTimeOffset? LockedUntilUtc { get; set; }
        }

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var normalizedUsername = username.Trim();
            if (IsLockedOut(normalizedUsername))
            {
                return null;
            }

            var user = await _userRepository.GetUserByUsernameAsync(normalizedUsername);
            if (user == null || !user.IsActive)
            {
                RegisterFailedAttempt(normalizedUsername);
                return null;
            }

            try
            {
                var salt = Convert.FromBase64String(user.Salt);
                var expectedHash = Convert.FromBase64String(user.PasswordHash);
                var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

                var success = expectedHash.Length == hashBytes.Length &&
                              CryptographicOperations.FixedTimeEquals(hashBytes, expectedHash);
                if (success)
                {
                    FailedLogins.TryRemove(normalizedUsername, out _);
                    return user;
                }

                RegisterFailedAttempt(normalizedUsername);
                return null;
            }
            catch (FormatException)
            {
                RegisterFailedAttempt(normalizedUsername);
                return null;
            }
        }

        public (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        private static bool IsLockedOut(string username)
        {
            if (!FailedLogins.TryGetValue(username, out var state) || state.LockedUntilUtc is null)
            {
                return false;
            }

            if (state.LockedUntilUtc > DateTimeOffset.UtcNow)
            {
                return true;
            }

            FailedLogins.TryRemove(username, out _);
            return false;
        }

        private static void RegisterFailedAttempt(string username)
        {
            FailedLogins.AddOrUpdate(
                username,
                _ => new FailedLoginState
                {
                    Attempts = 1
                },
                (_, existing) =>
                {
                    if (existing.LockedUntilUtc is not null && existing.LockedUntilUtc <= DateTimeOffset.UtcNow)
                    {
                        existing.Attempts = 0;
                        existing.LockedUntilUtc = null;
                    }

                    existing.Attempts += 1;
                    if (existing.Attempts >= MaxFailedAttempts)
                    {
                        existing.LockedUntilUtc = DateTimeOffset.UtcNow.Add(LockoutWindow);
                    }

                    return existing;
                });
        }
    }
}
