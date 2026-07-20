using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using System.Security.Cryptography;
using AutoMapper;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record AddUserCommand(
        string Username,
        string Password,
        string Role,
        int RoleId
    ) : IRequest<(bool Success, string Error)>;

    public class AddUserCommandHandler : IRequestHandler<AddUserCommand, (bool Success, string Error)>
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private const int Iterations = 100000;
        private const int KeySize = 32;
        private const int SaltSize = 16;

        public AddUserCommandHandler(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<(bool Success, string Error)> Handle(AddUserCommand request, CancellationToken cancellationToken)
        {
            var existing = await _userRepository.GetUserByUsernameAsync(request.Username.Trim());
            if (existing != null)
                return (false, $"Username '{request.Username}' already exists.");

            var (hash, salt) = HashPassword(request.Password);
            var user = _mapper.Map<User>(request);
            user.PasswordHash = hash;
            user.Salt = salt;

            await _userRepository.AddAsync(user);
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
