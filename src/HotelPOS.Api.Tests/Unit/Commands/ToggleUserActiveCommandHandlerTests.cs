using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class ToggleUserActiveCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly ToggleUserActiveCommandHandler _handler;

        public ToggleUserActiveCommandHandlerTests()
        {
            _handler = new ToggleUserActiveCommandHandler(_userRepo.Object);
        }

        [Fact]
        public async Task Handle_Deactivating_SetsIsActiveFalse()
        {
            var user = new User { Id = 1, IsActive = true };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _handler.Handle(new ToggleUserActiveCommand(1, false), CancellationToken.None);

            Assert.False(user.IsActive);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_Reactivating_SetsIsActiveTrue()
        {
            var user = new User { Id = 1, IsActive = false };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _handler.Handle(new ToggleUserActiveCommand(1, true), CancellationToken.None);

            Assert.True(user.IsActive);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsAndDoesNotUpdate()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _handler.Handle(new ToggleUserActiveCommand(99, false), CancellationToken.None));

            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
