using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class UserServiceAuthorizationTests
    {
        private readonly Mock<IUserRepository> _repo = new();

        [Fact]
        public async Task AddUserAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = new UserService(_repo.Object, auth.Object, Mock.Of<IUserContext>(), isTest: true);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AddUserAsync("newuser", "ValidPass123!", RoleNames.Cashier, 2));
        }

        [Fact]
        public async Task ResetPasswordAsync_AllowsSelfServiceWithoutSettingsPermission()
        {
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsureSelfOrPermission(5, "Settings"));

            var authService = new AuthService(_repo.Object, new HotelPOS.Tests.TestHelpers.InMemoryLoginLockoutRepository());
            var (hash, salt) = authService.HashPassword("OldPassword1!");
            var user = new User { Id = 5, PasswordHash = hash, Salt = salt };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var userContext = new Mock<IUserContext>();
            userContext.Setup(u => u.CurrentUserId).Returns(5);

            var service = new UserService(_repo.Object, auth.Object, userContext.Object, isTest: true);
            var (success, error) = await service.ResetPasswordAsync(5, "NewPassword1!", "OldPassword1!");

            Assert.True(success, error);
            auth.Verify(a => a.EnsureSelfOrPermission(5, "Settings"), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_SelfServiceWithWrongCurrentPassword_Fails()
        {
            var auth = TestAuthorization.AllowAll();

            var authService = new AuthService(_repo.Object, new HotelPOS.Tests.TestHelpers.InMemoryLoginLockoutRepository());
            var (hash, salt) = authService.HashPassword("OldPassword1!");
            var user = new User { Id = 5, PasswordHash = hash, Salt = salt };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var userContext = new Mock<IUserContext>();
            userContext.Setup(u => u.CurrentUserId).Returns(5);

            var service = new UserService(_repo.Object, auth.Object, userContext.Object, isTest: true);
            var (success, error) = await service.ResetPasswordAsync(5, "NewPassword1!", "WrongPassword!");

            Assert.False(success);
            Assert.Equal("Current password is incorrect.", error);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_AdminResetsAnotherUser_DoesNotRequireCurrentPassword()
        {
            var auth = TestAuthorization.AllowAll();

            var user = new User { Id = 6, PasswordHash = "irrelevant-hash", Salt = "irrelevant-salt" };
            _repo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(user);

            // Caller is user 1 (an admin), target is user 6 - not self-service.
            var userContext = new Mock<IUserContext>();
            userContext.Setup(u => u.CurrentUserId).Returns(1);

            var service = new UserService(_repo.Object, auth.Object, userContext.Object, isTest: true);
            var (success, error) = await service.ResetPasswordAsync(6, "NewPassword1!");

            Assert.True(success, error);
        }

        [Fact]
        public async Task GetAllUsersAsync_WhenAuthorized_Succeeds()
        {
            var auth = TestAuthorization.AllowAll();
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var service = new UserService(_repo.Object, auth.Object, Mock.Of<IUserContext>(), isTest: true);
            var users = await service.GetAllUsersAsync();

            Assert.Empty(users);
        }
    }
}

