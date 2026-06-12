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
            var service = new UserService(_repo.Object, auth.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.AddUserAsync("newuser", "ValidPass123!", "Cashier", 2));
        }

        [Fact]
        public async Task ResetPasswordAsync_AllowsSelfServiceWithoutSettingsPermission()
        {
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsureSelfOrPermission(5, "Settings"));

            var user = new User { Id = 5, PasswordHash = "old", Salt = "old" };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var service = new UserService(_repo.Object, auth.Object);
            var (success, _) = await service.ResetPasswordAsync(5, "NewPassword1!");

            Assert.True(success);
            auth.Verify(a => a.EnsureSelfOrPermission(5, "Settings"), Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_WhenAuthorized_Succeeds()
        {
            var auth = TestAuthorization.AllowAll();
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var service = new UserService(_repo.Object, auth.Object);
            var users = await service.GetAllUsersAsync();

            Assert.Empty(users);
        }
    }
}
