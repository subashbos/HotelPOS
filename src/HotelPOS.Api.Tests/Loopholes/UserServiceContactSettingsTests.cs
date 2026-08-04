using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers UserService.SetTwoFactorAsync and SetEmailAsync (legacy repository-backed
    /// constructor path) — previously untested despite the mediator-DI path being exercised
    /// via SetTwoFactorCommandHandlerTests/SetUserEmailCommandHandlerTests.
    /// </summary>
    public class UserServiceContactSettingsTests
    {
        private readonly Mock<IUserRepository> _repo = new();

        private UserService BuildService(Mock<IAuthorizationService>? auth = null, int? currentUserId = null)
        {
            var userContext = new Mock<IUserContext>();
            userContext.Setup(u => u.CurrentUserId).Returns(currentUserId);
            return new UserService(_repo.Object, (auth ?? TestAuthorization.AllowAll()).Object, userContext.Object, isTest: true);
        }

        // ── SetTwoFactorAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task SetTwoFactorAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((User?)null);
            var service = BuildService();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.SetTwoFactorAsync(404, true, "SECRET"));
        }

        [Fact]
        public async Task SetTwoFactorAsync_Enable_SetsFlagAndSecret()
        {
            var user = new User { Id = 1, TwoFactorEnabled = false, TwoFactorSecret = null };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            var service = BuildService();

            await service.SetTwoFactorAsync(1, true, "NEWSECRET");

            Assert.True(user.TwoFactorEnabled);
            Assert.Equal("NEWSECRET", user.TwoFactorSecret);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task SetTwoFactorAsync_Disable_ClearsSecretEvenIfProvided()
        {
            var user = new User { Id = 2, TwoFactorEnabled = true, TwoFactorSecret = "OLDSECRET" };
            _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
            var service = BuildService();

            // A stray secret value being passed alongside enabled=false must not survive.
            await service.SetTwoFactorAsync(2, false, "IGNORED");

            Assert.False(user.TwoFactorEnabled);
            Assert.Null(user.TwoFactorSecret);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task SetTwoFactorAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.SetTwoFactorAsync(1, true, "SECRET"));

            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SetTwoFactorAsync_SelfService_DoesNotRequireSettingsPermission()
        {
            var auth = new Mock<IAuthorizationService>();
            auth.Setup(a => a.EnsureSelfOrPermission(7, PermissionModules.Settings));

            var user = new User { Id = 7, TwoFactorEnabled = false };
            _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(user);
            var service = BuildService(auth, currentUserId: 7);

            await service.SetTwoFactorAsync(7, true, "SECRET");

            auth.Verify(a => a.EnsureSelfOrPermission(7, PermissionModules.Settings), Times.Once);
            Assert.True(user.TwoFactorEnabled);
        }

        // ── SetEmailAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task SetEmailAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((User?)null);
            var service = BuildService();

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.SetEmailAsync(404, "a@b.com"));
        }

        [Fact]
        public async Task SetEmailAsync_ValidEmail_SetsEmailAndUpdates()
        {
            var user = new User { Id = 3, Email = null };
            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(user);
            var service = BuildService();

            await service.SetEmailAsync(3, "new@hotel.com");

            Assert.Equal("new@hotel.com", user.Email);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task SetEmailAsync_NullEmail_ClearsEmail()
        {
            var user = new User { Id = 4, Email = "old@hotel.com" };
            _repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(user);
            var service = BuildService();

            await service.SetEmailAsync(4, null);

            Assert.Null(user.Email);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task SetEmailAsync_WhenUnauthorized_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = BuildService(auth);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.SetEmailAsync(1, "a@b.com"));

            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
