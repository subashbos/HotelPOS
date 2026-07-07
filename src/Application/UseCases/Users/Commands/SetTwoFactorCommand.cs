using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Commands
{
    public record SetTwoFactorCommand(int UserId, bool Enabled, string? Secret) : IRequest;

    public class SetTwoFactorCommandHandler : IRequestHandler<SetTwoFactorCommand>
    {
        private readonly IUserRepository _userRepository;

        public SetTwoFactorCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task Handle(SetTwoFactorCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new KeyNotFoundException($"User #{request.UserId} not found.");

            user.TwoFactorEnabled = request.Enabled;
            user.TwoFactorSecret = request.Enabled ? request.Secret : null;
            await _userRepository.UpdateAsync(user);
        }
    }
}
