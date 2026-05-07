using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _service = new AuthService(_userRepoMock.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_Should_Lockout_After_5_Failed_Attempts()
        {
            // Arrange
            var username = "lockout_test_" + Guid.NewGuid(); // Use unique username to avoid static pollution
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((User?)null);

            // Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var result = await _service.AuthenticateAsync(username, "wrong_password");
                Assert.Null(result);
            }

            // The 6th attempt should be blocked by IsLockedOut without even calling the repo
            var lockedResult = await _service.AuthenticateAsync(username, "wrong_password");
            Assert.Null(lockedResult);

            // Verify repo was only called 5 times (the 6th was blocked early)
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(username), Times.Exactly(5));
        }

        [Fact]
        public async Task AuthenticateAsync_Should_Succeed_And_Clear_Attempts_On_Correct_Password()
        {
            // Arrange
            var username = "success_test_" + Guid.NewGuid();
            var (hash, salt) = _service.HashPassword("correct");
            var user = new User { Username = username, PasswordHash = hash, Salt = salt, IsActive = true };

            _userRepoMock.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(user);

            // 1. Fail 3 times
            for (int i = 0; i < 3; i++)
            {
                await _service.AuthenticateAsync(username, "wrong");
            }

            // 2. Succeed
            var result = await _service.AuthenticateAsync(username, "correct");
            Assert.NotNull(result);
            Assert.Equal(username, result.Username);

            // 3. Verify that we are NOT locked out now (next 2 fails won't trigger lockout)
            _userRepoMock.Invocations.Clear();
            for (int i = 0; i < 2; i++)
            {
                await _service.AuthenticateAsync(username, "wrong");
            }

            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(username), Times.Exactly(2));
        }
    }
}
