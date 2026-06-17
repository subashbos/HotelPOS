using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record DeleteUserCommand(int UserId, int CurrentUserId) : IRequest;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IUserRepository _userRepository;

        public DeleteUserCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.CurrentUserId)
                throw new InvalidOperationException("You cannot delete your own account.");

            _ = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new KeyNotFoundException($"User #{request.UserId} not found.");

            await _userRepository.DeleteAsync(request.UserId);
        }
    }
}
