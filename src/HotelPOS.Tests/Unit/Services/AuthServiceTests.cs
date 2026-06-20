using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
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

        [Fact]
        public async Task AuthenticateAsync_Should_Fail_If_User_Is_Inactive()
        {
            // Arrange
            var username = "inactive_user";
            var (hash, salt) = _service.HashPassword("password");
            var user = new User { Username = username, PasswordHash = hash, Salt = salt, IsActive = false };

            _userRepoMock.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(username, "password");

            // Assert
            Assert.Null(result); // Should fail even with correct password because IsActive is false
        }

        [Fact]
        public async Task AuthenticateAsync_LockoutWindowExpired_ResetsLockoutAndAllowsLogin()
        {
            // Arrange
            var username = "expiry_test_" + Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((User?)null);

            // 1. Fail 5 times to trigger lockout
            for (int i = 0; i < 5; i++)
            {
                await _service.AuthenticateAsync(username, "wrong_password");
            }

            // Verify currently locked out
            var lockedResult = await _service.AuthenticateAsync(username, "wrong_password");
            Assert.Null(lockedResult);
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(username), Times.Exactly(5));

            // 2. Use reflection to adjust LockedUntilUtc in the past
            var failedLoginsField = typeof(AuthService).GetField("FailedLogins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var failedLogins = failedLoginsField?.GetValue(null) as System.Collections.IDictionary;
            Assert.NotNull(failedLogins);

            object? state = null;
            foreach (System.Collections.DictionaryEntry entry in failedLogins!)
            {
                if (entry.Key?.ToString()?.Equals(username, StringComparison.OrdinalIgnoreCase) == true)
                {
                    state = entry.Value;
                    break;
                }
            }
            Assert.NotNull(state);

            var stateType = typeof(AuthService).GetNestedType("FailedLoginState", System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(stateType);
            var constructor = stateType!.GetConstructor(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new[] { typeof(int), typeof(DateTimeOffset?) }, null);
            Assert.NotNull(constructor);
            var newState = constructor!.Invoke(new object?[] { 5, DateTimeOffset.UtcNow.AddMinutes(-10) });
            failedLogins[username] = newState;

            // 3. Try to authenticate again. The lockout should be cleared and repo is queried again
            _userRepoMock.Invocations.Clear();
            var afterExpiryResult = await _service.AuthenticateAsync(username, "wrong_password");
            Assert.Null(afterExpiryResult);
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(username), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_ConcurrentFailedLogins_HandlesConcurrencyGracefully()
        {
            // Arrange
            var username = "concurrent_test_" + Guid.NewGuid();
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync((User?)null);

            // Act
            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => _service.AuthenticateAsync(username, "wrong")))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert - subsequent login attempts should be locked out
            _userRepoMock.Invocations.Clear();
            var lockedResult = await _service.AuthenticateAsync(username, "wrong");
            Assert.Null(lockedResult);
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(username), Times.Never);
        }
    }
}

