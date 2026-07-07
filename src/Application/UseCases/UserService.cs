using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Application.UseCases.Users.Queries;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class UserService : IUserService
    {
        private readonly IMediator? _mediator;
        private readonly IUserRepository? _userRepository;
        private readonly IAuthorizationService _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public UserService(IMediator mediator, IAuthorizationService authorization)
        {
            _mediator = mediator;
            _authorization = authorization;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public UserService(IUserRepository userRepository, IAuthorizationService authorization, bool isTest)
        {
            _userRepository = userRepository;
            _authorization = authorization;
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            _authorization.EnsurePermission(PermissionModules.Settings);

            if (_mediator != null)
                return _mediator.Send(new GetAllUsersQuery());

            return _userRepository!.GetAllAsync();
        }

        public async Task<(bool Success, string Error)> AddUserAsync(string username, string password, string role, int roleId)
        {
            _authorization.EnsurePermission(PermissionModules.Settings);

            if (_mediator != null)
                return await _mediator.Send(new AddUserCommand(username, password, role, roleId));

            // Legacy path — basic duplicate check and basic validations
            if (string.IsNullOrWhiteSpace(username))
                return (false, "Username cannot be empty.");
            if (string.IsNullOrEmpty(password))
                return (false, $"Password cannot be empty. Password must be at least {ValidationLimits.MinPasswordLength} characters.");
            if (password.Length < ValidationLimits.MinPasswordLength)
                return (false, $"Password must be at least {ValidationLimits.MinPasswordLength} characters.");

            var existing = await _userRepository!.GetUserByUsernameAsync(username.Trim());
            if (existing != null)
                return (false, $"Username '{username}' already exists.");

            // Mimic AddUserCommandHandler for legacy tests to hash password and save
            var (hash, salt) = HashPassword(password);
            var user = new User
            {
                Username = username.Trim(),
                PasswordHash = hash,
                Salt = salt,
                Role = role,
                RoleId = roleId,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            return (true, string.Empty);
        }

        private static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = new byte[ValidationLimits.SaltByteSize];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(saltBytes);
            var hashBytes = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, ValidationLimits.Pbkdf2Iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, ValidationLimits.HashByteSize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public async Task ToggleActiveAsync(int userId, bool isActive)
        {
            _authorization.EnsurePermission(PermissionModules.Settings);

            if (_mediator != null)
            {
                await _mediator.Send(new ToggleUserActiveCommand(userId, isActive));
                return;
            }

            var user = await _userRepository!.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User #{userId} not found.");
            user.IsActive = isActive;
            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(int userId, int currentUserId)
        {
            _authorization.EnsurePermission(PermissionModules.Settings);

            if (_mediator != null)
            {
                await _mediator.Send(new DeleteUserCommand(userId, currentUserId));
                return;
            }

            if (userId == currentUserId)
                throw new InvalidOperationException("You cannot delete your own account.");

            var user = await _userRepository!.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User #{userId} not found.");
            await _userRepository.DeleteAsync(userId);
        }

        public async Task<(bool Success, string Error)> ResetPasswordAsync(int userId, string newPassword)
        {
            _authorization.EnsureSelfOrPermission(userId, PermissionModules.Settings);

            if (_mediator != null)
                return await _mediator.Send(new ResetPasswordCommand(userId, newPassword));

            if (string.IsNullOrEmpty(newPassword))
                return (false, $"New password cannot be empty. Password must be at least {ValidationLimits.MinPasswordLength} characters.");
            if (newPassword.Length < ValidationLimits.MinPasswordLength)
                return (false, $"Password must be at least {ValidationLimits.MinPasswordLength} characters.");

            var user = await _userRepository!.GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.MustChangePassword = false;
            await _userRepository.UpdateAsync(user);

            return (true, string.Empty);
        }
    }
}
