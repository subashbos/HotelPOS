using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using MediatR;
using HotelPOS.Application.UseCases.Auth.Commands;

namespace HotelPOS.Application.UseCases
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginLockoutRepository _lockoutRepository;
        private readonly IMediator? _mediator;
        private readonly IAuditService? _auditService;

        // PBKDF2 Configurations
        private const int Iterations = ValidationLimits.Pbkdf2Iterations;
        private const int KeySize = ValidationLimits.HashByteSize;
        private const int SaltSize = ValidationLimits.SaltByteSize;
        private const int MaxFailedAttempts = SecurityDefaults.MaxFailedLoginAttempts;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(SecurityDefaults.LockoutWindowMinutes);

        // Per-username in-process gate: serializes the read-modify-write against the
        // lockout store so concurrent attempts from the same app instance can't lose
        // updates to each other. Persistence for surviving restarts lives in the DB.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> LockoutGates =
            new(StringComparer.OrdinalIgnoreCase);

        public AuthService(
            IUserRepository userRepository,
            ILoginLockoutRepository lockoutRepository,
            IMediator? mediator = null,
            IAuditService? auditService = null)
        {
            _userRepository = userRepository;
            _lockoutRepository = lockoutRepository;
            _mediator = mediator;
            _auditService = auditService;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (_mediator != null)
            {
                return await _mediator.Send(new LoginCommand(username, password));
            }

            return await AuthenticateInternalAsync(username, password);
        }

        public async Task<User?> AuthenticateInternalAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return null;

            var normalizedUsername = username.Trim();
            var lockoutKey = normalizedUsername.ToLowerInvariant();

            if (await IsLockedOutAsync(lockoutKey))
            {
                return null;
            }

            var user = await _userRepository.GetUserByUsernameAsync(normalizedUsername);
            if (user == null || !user.IsActive)
            {
                await RegisterFailedAttemptAsync(lockoutKey);
                await LogAuditAsync(user?.Id ?? 0, normalizedUsername, AuditActions.LoginFailed,
                    user == null ? "Unknown username" : "Account inactive");
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
                    await _lockoutRepository.ClearAsync(lockoutKey);
                    user.LastLoginUtc = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                    await LogAuditAsync(user.Id, user.Username, AuditActions.LoginSuccess, null);
                    return user;
                }

                await RegisterFailedAttemptAsync(lockoutKey);
                await LogAuditAsync(user.Id, user.Username, AuditActions.LoginFailed, "Invalid password");
                return null;
            }
            catch (FormatException)
            {
                await RegisterFailedAttemptAsync(lockoutKey);
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

        private static SemaphoreSlim GetGate(string lockoutKey) =>
            LockoutGates.GetOrAdd(lockoutKey, _ => new SemaphoreSlim(1, 1));

        private async Task<bool> IsLockedOutAsync(string lockoutKey)
        {
            var gate = GetGate(lockoutKey);
            await gate.WaitAsync();
            try
            {
                var state = await _lockoutRepository.GetAsync(lockoutKey);
                if (state?.LockedUntilUtc is null)
                {
                    return false;
                }

                if (state.LockedUntilUtc > DateTime.UtcNow)
                {
                    return true;
                }

                await _lockoutRepository.ClearAsync(lockoutKey);
                return false;
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task RegisterFailedAttemptAsync(string lockoutKey)
        {
            var gate = GetGate(lockoutKey);
            await gate.WaitAsync();
            try
            {
                var state = await _lockoutRepository.GetAsync(lockoutKey)
                            ?? new LoginLockout { NormalizedUsername = lockoutKey };

                if (state.LockedUntilUtc is not null && state.LockedUntilUtc <= DateTime.UtcNow)
                {
                    state.FailedAttempts = 0;
                    state.LockedUntilUtc = null;
                }

                state.FailedAttempts += 1;
                state.LastAttemptUtc = DateTime.UtcNow;
                if (state.FailedAttempts >= MaxFailedAttempts)
                {
                    state.LockedUntilUtc = DateTime.UtcNow.Add(LockoutWindow);
                }

                await _lockoutRepository.SaveAsync(state);
            }
            finally
            {
                gate.Release();
            }
        }

        private async Task LogAuditAsync(int userId, string username, string action, string? details)
        {
            if (_auditService == null) return;

            try
            {
                await _auditService.LogActionAsync("User", userId, action, details ?? $"{action}: {username}");
            }
            catch
            {
                // Auditing must never block authentication.
            }
        }
    }
}
