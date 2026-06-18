using HotelPOS.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;

namespace HotelPOS.Application.UseCases.Auth.Commands
{
    public record LoginCommand(string Username, string Password) : IRequest<User?>;

    public class LoginCommandHandler : IRequestHandler<LoginCommand, User?>
    {
        private readonly IAuthService _authService;

        public LoginCommandHandler(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<User?> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await _authService.AuthenticateInternalAsync(request.Username, request.Password);
        }
    }
}
