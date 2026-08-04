using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Entities;
using MediatR;
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

        private static (string Hash, string Salt) HashPassword(string password) => PasswordHasher.Hash(password);
    }
}
