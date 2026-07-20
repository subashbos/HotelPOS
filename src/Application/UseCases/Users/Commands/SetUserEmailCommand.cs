using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record SetUserEmailCommand(int UserId, string? Email) : IRequest;

    public class SetUserEmailCommandHandler : IRequestHandler<SetUserEmailCommand>
    {
        private readonly IUserRepository _userRepository;

        public SetUserEmailCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task Handle(SetUserEmailCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new KeyNotFoundException($"User #{request.UserId} not found.");

            user.Email = request.Email;
            await _userRepository.UpdateAsync(user);
        }
    }
}
