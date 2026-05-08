using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers UserService edge cases that were missing:
    /// null password reset, ToggleActive not-found, Delete not-found,
    /// self-delete guard, and GetAllUsers empty list.
    /// </summary>
    public class UserServiceLoopholeTests
    {
        private readonly Mock<IUserRepository> _repo = new();
        private readonly UserService _service;

        public UserServiceLoopholeTests()
        {
            _service = new UserService(_repo.Object);
        }

        // ── ResetPasswordAsync null/empty guard ──────────────────────────────

        [Fact]
        public async Task ResetPasswordAsync_NullPassword_ReturnsErrorWithoutThrowing()
        {
            var ex = await Record.ExceptionAsync(() => _service.ResetPasswordAsync(1, null!));
            Assert.Null(ex);

            var (success, error) = await _service.ResetPasswordAsync(1, null!);
            Assert.False(success);
            Assert.Contains("10", error);
        }

        [Fact]
        public async Task ResetPasswordAsync_EmptyPassword_ReturnsError()
        {
            var (success, error) = await _service.ResetPasswordAsync(1, "");
            Assert.False(success);
            Assert.Contains("10", error);
        }

        [Fact]
        public async Task ResetPasswordAsync_UserNotFound_ReturnsError()
        {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            var (success, error) = await _service.ResetPasswordAsync(99, "ValidPass123!");
            Assert.False(success);
            Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidPassword_UpdatesHashAndClearsFlag()
        {
            var user = new User { Id = 5, PasswordHash = "old", Salt = "old", MustChangePassword = true };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var (success, _) = await _service.ResetPasswordAsync(5, "NewSecure1234!");

            Assert.True(success);
            Assert.False(user.MustChangePassword);
            Assert.NotEqual("old", user.PasswordHash);
            Assert.NotEqual("old", user.Salt);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        // ── ToggleActiveAsync not-found ──────────────────────────────────────

        [Fact]
        public async Task ToggleActiveAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.ToggleActiveAsync(999, true));
        }

        [Fact]
        public async Task ToggleActiveAsync_ValidUser_SetsIsActiveTrue()
        {
            var user = new User { Id = 1, IsActive = false };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            await _service.ToggleActiveAsync(1, true);

            Assert.True(user.IsActive);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ToggleActiveAsync_ValidUser_SetsIsActiveFalse()
        {
            var user = new User { Id = 2, IsActive = true };
            _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);

            await _service.ToggleActiveAsync(2, false);

            Assert.False(user.IsActive);
            _repo.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        // ── DeleteUserAsync not-found ────────────────────────────────────────

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            _repo.Setup(r => r.GetByIdAsync(888)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteUserAsync(888, currentUserId: 1));
        }

        [Fact]
        public async Task DeleteUserAsync_SelfDelete_ThrowsInvalidOperationException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteUserAsync(userId: 3, currentUserId: 3));

            // Repo should never be called
            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUserAsync_ValidUser_CallsRepoDelete()
        {
            var user = new User { Id = 10 };
            _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(user);

            await _service.DeleteUserAsync(userId: 10, currentUserId: 1);

            _repo.Verify(r => r.DeleteAsync(10), Times.Once);
        }

        // ── GetAllUsersAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetAllUsersAsync_EmptyRepo_ReturnsEmptyList()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

            var result = await _service.GetAllUsersAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            var users = new List<User>
            {
                new User { Id = 1, Username = "admin" },
                new User { Id = 2, Username = "cashier" }
            };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _service.GetAllUsersAsync();

            Assert.Equal(2, result.Count);
        }

        // ── AddUserAsync edge cases ──────────────────────────────────────────

        [Fact]
        public async Task AddUserAsync_UsernameWithOnlySpaces_ReturnsError()
        {
            var (success, error) = await _service.AddUserAsync("   ", "ValidPass123!", "Cashier", 2);
            Assert.False(success);
            Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddUserAsync_PasswordExactlyMinLength_Succeeds()
        {
            _repo.Setup(r => r.GetUserByUsernameAsync("newuser")).ReturnsAsync((User?)null);

            // Exactly 10 characters
            var (success, _) = await _service.AddUserAsync("newuser", "1234567890", "Cashier", 2);
            Assert.True(success);
        }

        [Fact]
        public async Task AddUserAsync_PasswordOneLessThanMin_ReturnsError()
        {
            // 9 characters — one short
            var (success, error) = await _service.AddUserAsync("newuser", "123456789", "Cashier", 2);
            Assert.False(success);
            Assert.Contains("10", error);
        }
    }
}
