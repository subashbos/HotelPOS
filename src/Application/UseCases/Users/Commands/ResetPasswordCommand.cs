using HotelPOS.Application.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record ResetPasswordCommand(int UserId, string NewPassword) : IRequest<(bool Success, string Error)>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, (bool Success, string Error)>
    {
        private readonly IUserRepository _userRepository;
        private const int Iterations = 100000;
        private const int KeySize = 32;
        private const int SaltSize = 16;

        public ResetPasswordCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<(bool Success, string Error)> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId);
            if (user == null) return (false, "User not found.");

            var (hash, salt) = HashPassword(request.NewPassword);
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
