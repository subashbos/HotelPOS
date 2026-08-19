#nullable enable

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
        private readonly IUserContext _userContext;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public UserService(IMediator mediator, IAuthorizationService authorization, IUserContext userContext)
        {
            _mediator = mediator;
            _authorization = authorization;
            _userContext = userContext;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public UserService(IUserRepository userRepository, IAuthorizationService authorization, IUserContext userContext, bool isTest)
        {
            _userRepository = userRepository;
            _authorization = authorization;
            _userContext = userContext;
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

        private static (string Hash, string Salt) HashPassword(string password) => HotelPOS.Domain.Common.PasswordHasher.Hash(password);

        private static bool VerifyPassword(string password, string hash, string salt) => HotelPOS.Domain.Common.PasswordHasher.Verify(password, hash, salt);

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

        public async Task SetTwoFactorAsync(int userId, bool enabled, string? secret)
        {
            _authorization.EnsureSelfOrPermission(userId, PermissionModules.Settings);

            if (_mediator != null)
            {
                await _mediator.Send(new SetTwoFactorCommand(userId, enabled, secret));
                return;
            }

            var user = await _userRepository!.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User #{userId} not found.");
            user.TwoFactorEnabled = enabled;
            user.TwoFactorSecret = enabled ? secret : null;
            await _userRepository.UpdateAsync(user);
        }

        public async Task SetEmailAsync(int userId, string? email)
        {
            _authorization.EnsurePermission(PermissionModules.Settings);

            if (_mediator != null)
            {
                await _mediator.Send(new SetUserEmailCommand(userId, email));
                return;
            }

            var user = await _userRepository!.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User #{userId} not found.");
            user.Email = email;
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

            _ = await _userRepository!.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User #{userId} not found.");
            await _userRepository.DeleteAsync(userId);
        }

        public async Task<(bool Success, string Error)> ResetPasswordAsync(int userId, string newPassword, string? currentPassword = null)
        {
            _authorization.EnsureSelfOrPermission(userId, PermissionModules.Settings);

            if (_mediator != null)
                return await _mediator.Send(new ResetPasswordCommand(userId, newPassword, currentPassword));

            if (string.IsNullOrEmpty(newPassword))
                return (false, $"New password cannot be empty. Password must be at least {ValidationLimits.MinPasswordLength} characters.");
            if (newPassword.Length < ValidationLimits.MinPasswordLength)
                return (false, $"Password must be at least {ValidationLimits.MinPasswordLength} characters.");

            var user = await _userRepository!.GetByIdAsync(userId);
            if (user == null) return (false, "User not found.");

            // Same self-service current-password proof as ResetPasswordCommandHandler (the
            // mediator-DI path above) - see that handler for the full rationale.
            if (_userContext.CurrentUserId == userId && (string.IsNullOrEmpty(currentPassword) || !VerifyPassword(currentPassword, user.PasswordHash, user.Salt)))
            {
                return (false, "Current password is incorrect.");
            }

            var (hash, salt) = HashPassword(newPassword);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.MustChangePassword = false;
            await _userRepository.UpdateAsync(user);

            return (true, string.Empty);
        }
    }
}
