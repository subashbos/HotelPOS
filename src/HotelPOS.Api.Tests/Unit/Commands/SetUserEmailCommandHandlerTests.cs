using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Users.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class SetUserEmailCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly SetUserEmailCommandHandler _handler;

        public SetUserEmailCommandHandlerTests()
        {
            _handler = new SetUserEmailCommandHandler(_userRepo.Object);
        }

        [Fact]
        public async Task Handle_SetsEmailAndPersists()
        {
            var user = new User { Id = 1, Email = null };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _handler.Handle(new SetUserEmailCommand(1, "new@hotel.com"), CancellationToken.None);

            Assert.Equal("new@hotel.com", user.Email);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_NullEmail_ClearsExistingEmail()
        {
            var user = new User { Id = 1, Email = "old@hotel.com" };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _handler.Handle(new SetUserEmailCommand(1, null), CancellationToken.None);

            Assert.Null(user.Email);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task Handle_UserNotFound_ThrowsAndDoesNotUpdate()
        {
            _userRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _handler.Handle(new SetUserEmailCommand(99, "x@y.com"), CancellationToken.None));

            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
