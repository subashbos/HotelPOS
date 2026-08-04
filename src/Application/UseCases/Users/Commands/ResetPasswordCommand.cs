using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record ResetPasswordCommand(int UserId, string NewPassword, string? CurrentPassword = null) : IRequest<(bool Success, string Error)>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, (bool Success, string Error)>
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserContext _userContext;

        public ResetPasswordCommandHandler(IUserRepository userRepository, IUserContext userContext)
        {
            _userRepository = userRepository;
            _userContext = userContext;
        }

        public async Task<(bool Success, string Error)> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null) return (false, "User not found.");

            // Changing your OWN password must prove you still know the current one - otherwise a
            // hijacked or left-open session could lock the real owner out permanently. An admin
            // resetting someone ELSE's forgotten password can't supply their target's current
            // password (that's the whole point of an admin-assisted reset), so this only applies
            // when the caller is the account being changed; EnsureSelfOrPermission upstream already
            // gated who is allowed to call this at all.
            if (_userContext.CurrentUserId == request.UserId &&
                (string.IsNullOrEmpty(request.CurrentPassword) ||
                 !VerifyPassword(request.CurrentPassword, user.PasswordHash, user.Salt)))
            {
                return (false, "Current password is incorrect.");
            }

            var (hash, salt) = HashPassword(request.NewPassword);
            user.PasswordHash = hash;
            user.Salt = salt;
            user.MustChangePassword = false;
            await _userRepository.UpdateAsync(user);
            return (true, string.Empty);
        }

        private static (string Hash, string Salt) HashPassword(string password) => PasswordHasher.Hash(password);

        private static bool VerifyPassword(string password, string hash, string salt) => PasswordHasher.Verify(password, hash, salt);
    }
}
