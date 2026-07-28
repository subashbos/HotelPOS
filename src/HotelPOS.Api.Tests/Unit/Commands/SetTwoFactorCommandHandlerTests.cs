using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class SetTwoFactorCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly SetTwoFactorCommandHandler _handler;

        public SetTwoFactorCommandHandlerTests()
        {
            _handler = new SetTwoFactorCommandHandler(_userRepo.Object);
        }

        [Fact]
        public async Task Handle_Enabling_SetsFlagAndSecret()
        {
            var user = new User { Id = 1, TwoFactorEnabled = false, TwoFactorSecret = null };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _handler.Handle(new SetTwoFactorCommand(1, true, "SECRET123"), CancellationToken.None);

            Assert.True(user.TwoFactorEnabled);
            Assert.Equal("SECRET123", user.TwoFactorSecret);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_Disabling_ClearsSecretEvenIfProvided()
        {
            var user = new User { Id = 1, TwoFactorEnabled = true, TwoFactorSecret = "OLDSECRET" };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            // A stale/irrelevant secret in the request must never survive a disable.
            await _handler.Handle(new SetTwoFactorCommand(1, false, "IGNORED"), CancellationToken.None);

            Assert.False(user.TwoFactorEnabled);
            Assert.Null(user.TwoFactorSecret);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsAndDoesNotUpdate()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _handler.Handle(new SetTwoFactorCommand(99, true, "SECRET"), CancellationToken.None));

            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
