using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Auth.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IAuthService> _authService = new();
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(_authService.Object);
        }

        [Fact]
        public async Task Handle_DelegatesToAuthServiceAndReturnsUser()
        {
            var user = new User { Id = 1, Username = "admin" };
            _authService.Setup(a => a.AuthenticateInternalAsync("admin", "Sup3rSecret!x")).ReturnsAsync(user);

            var result = await _handler.Handle(new LoginCommand("admin", "Sup3rSecret!x"), CancellationToken.None);

            Assert.Same(user, result);
        }

        [Fact]
        public async Task Handle_InvalidCredentials_ReturnsNull()
        {
            _authService.Setup(a => a.AuthenticateInternalAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            var result = await _handler.Handle(new LoginCommand("admin", "wrong"), CancellationToken.None);

            Assert.Null(result);
        }
    }
}
