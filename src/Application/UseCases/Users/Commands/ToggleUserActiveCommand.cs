using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record ToggleUserActiveCommand(int UserId, bool IsActive) : IRequest;

    public class ToggleUserActiveCommandHandler : IRequestHandler<ToggleUserActiveCommand>
    {
        private readonly IUserRepository _userRepository;

        public ToggleUserActiveCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task Handle(ToggleUserActiveCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new KeyNotFoundException($"User #{request.UserId} not found.");

            user.IsActive = request.IsActive;
            await _userRepository.UpdateAsync(user);
        }
    }
}
