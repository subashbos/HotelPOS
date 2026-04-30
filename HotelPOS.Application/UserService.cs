using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using System.Security.Cryptography;

namespace HotelPOS.Application
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private const int MinimumPasswordLength = 10;

        // PBKDF2 — same parameters as AuthService for compatibility
        private const int Iterations = 100000;
        private const int KeySize = 32;   // 256-bit
        private const int SaltSize = 16;  // 128-bit

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public Task<List<User>> GetAllUsersAsync() => _userRepository.GetAllAsync();

        public async Task<(bool Success, string Error)> AddUserAsync(string username, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty.");
            if (password.Length < MinimumPasswordLength)
                return (false, $"Password must be at least {MinimumPasswordLength} characters.");
            if (role != "Admin" && role != "Cashier")
                return (false, "Role must be Admin or Cashier.");

            var existing = await _userRepository.GetUserByUsernameAsync(username.Trim());
            if (existing != null)
                return (false, $"Username '{username}' already exists.");

            var (hash, salt) = HashPassword(password);
            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = hash,
                Salt = salt,
                Role = role,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            return (true, string.Empty);
        }

        public async Task ToggleActiveAsync(int userId, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                await _userRepository.UpdateAsync(user);
            }
        }

        public async Task DeleteUserAsync(int userId, int currentUserId)
        {
            if (userId == currentUserId)
                throw new InvalidOperationException("You cannot delete your own account.");
            await _userRepository.DeleteAsync(userId);
        }

        public async Task<(bool Success, string Error)> ResetPasswordAsync(int userId, string newPassword)
        {
            if (newPassword.Length < MinimumPasswordLength)
                return (false, $"Password must be at least {MinimumPasswordLength} characters.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.MustChangePassword = false;
            await _userRepository.UpdateAsync(user);
            return (true, string.Empty);
        }

        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }
    }
}
