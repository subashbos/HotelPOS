using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Application.UseCases
{
    public class PasswordResetService : IPasswordResetService
    {
        private const int CodeLength = 6;
        private const int CodeValidityMinutes = 15;

        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetRepository _resetRepository;
        private readonly IEmailService _emailService;

        public PasswordResetService(
            IUserRepository userRepository,
            IPasswordResetRepository resetRepository,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _resetRepository = resetRepository;
            _emailService = emailService;
        }

        public async Task RequestResetAsync(string username)
        {
            var normalized = username?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(normalized)) return;

            var user = await _userRepository.GetUserByUsernameAsync(normalized);
            if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.Email))
            {
                return; // Never reveal whether the account or its email exists.
            }

            var code = GenerateNumericCode(CodeLength);
            await _resetRepository.AddAsync(new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = HashCode(code),
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(CodeValidityMinutes)
            });

            try
            {
                await _emailService.SendEmailAsync(
                    user.Email!,
                    "HotelPOS password reset code",
                    $"Your password reset code is {code}. It expires in {CodeValidityMinutes} minutes. " +
                    "If you didn't request this, you can safely ignore this email.");
            }
            catch (Exception ex)
            {
                // Never leak SMTP/configuration errors to an unauthenticated caller, but log
                // them so an admin can diagnose why a reset email didn't arrive.
                Log.Error(ex, "Failed to send password reset email for user {Username}", normalized);
            }
        }

        public async Task<(bool Success, string? Error)> ConfirmResetAsync(string username, string code, string newPassword)
        {
            var normalized = username?.Trim() ?? string.Empty;
            var user = await _userRepository.GetUserByUsernameAsync(normalized);
            if (user == null)
            {
                return (false, "This code is invalid or has expired. Request a new one.");
            }

            var request = await _resetRepository.GetLatestActiveAsync(user.Id);
            if (request == null || request.ExpiresUtc <= DateTime.UtcNow)
            {
                return (false, "This code is invalid or has expired. Request a new one.");
            }

            if (request.AttemptCount >= SecurityDefaults.MaxPasswordResetCodeAttempts)
            {
                // Burn the code so a lucky guess after the cap can't still succeed.
                request.Used = true;
                await _resetRepository.UpdateAsync(request);
                return (false, "This code is invalid or has expired. Request a new one.");
            }

            var providedHash = HashCode(code?.Trim() ?? string.Empty);
            if (!FixedTimeHashEquals(providedHash, request.CodeHash))
            {
                request.AttemptCount += 1;
                await _resetRepository.UpdateAsync(request);
                return (false, "This code is invalid or has expired. Request a new one.");
            }

            if (string.IsNullOrEmpty(newPassword) ||
                newPassword.Length < ValidationLimits.MinPasswordLength ||
                !PasswordPolicy.MeetsComplexityRequirements(newPassword))
            {
                return (false, PasswordPolicy.RequirementsMessage);
            }

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.MustChangePassword = false;
            await _userRepository.UpdateAsync(user);

            request.Used = true;
            await _resetRepository.UpdateAsync(request);

            return (true, null);
        }

        private static bool FixedTimeHashEquals(string a, string b)
        {
            var bytesA = Convert.FromBase64String(a);
            var bytesB = Convert.FromBase64String(b);
            return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
        }

        private static string GenerateNumericCode(int length)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (int i = 0; i < length; i++)
                chars[i] = (char)('0' + bytes[i] % 10);
            return new string(chars);
        }

        private static string HashCode(string code) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(ValidationLimits.SaltByteSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, ValidationLimits.Pbkdf2Iterations, HashAlgorithmName.SHA256, ValidationLimits.HashByteSize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }
    }
}
