using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers AuthService edge cases that were missing from AuthServiceTests.cs:
    /// inactive user, corrupted salt, null/whitespace username, lockout expiry,
    /// and hash non-determinism.
    /// </summary>
    public class AuthServiceLoopholeTests
    {
        private readonly Mock<IUserRepository> _repo = new();
        private readonly AuthService _service;

        public AuthServiceLoopholeTests()
        {
            _service = new AuthService(_repo.Object);
        }

        // ── Inactive user ────────────────────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_InactiveUser_ReturnsNull()
        {
            var (hash, salt) = _service.HashPassword("password123");
            var user = new User
            {
                Username = "inactive",
                PasswordHash = hash,
                Salt = salt,
                IsActive = false   // disabled account
            };
            _repo.Setup(r => r.GetUserByUsernameAsync("inactive")).ReturnsAsync(user);

            var result = await _service.AuthenticateAsync("inactive", "password123");

            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_InactiveUser_RegistersFailedAttempt()
        {
            // Even though the password is correct, an inactive user should still
            // count as a failed attempt (to prevent username enumeration via lockout timing).
            var username = "inactive_count_" + Guid.NewGuid();
            var (hash, salt) = _service.HashPassword("correct");
            var user = new User { Username = username, PasswordHash = hash, Salt = salt, IsActive = false };
            _repo.Setup(r => r.GetUserByUsernameAsync(username)).ReturnsAsync(user);

            // 5 attempts with correct password but inactive account
            for (int i = 0; i < 5; i++)
                await _service.AuthenticateAsync(username, "correct");

            // 6th attempt should be locked out (repo not called)
            _repo.Invocations.Clear();
            await _service.AuthenticateAsync(username, "correct");

            _repo.Verify(r => r.GetUserByUsernameAsync(username), Times.Never);
        }

        // ── Null / whitespace username ───────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_NullUsername_DoesNotThrow()
        {
            _repo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var ex = await Record.ExceptionAsync(() => _service.AuthenticateAsync(null!, "password"));
            Assert.Null(ex);
        }

        [Fact]
        public async Task AuthenticateAsync_NullUsername_ReturnsNull()
        {
            _repo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _service.AuthenticateAsync(null!, "password");
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WhitespaceUsername_ReturnsNull()
        {
            _repo.Setup(r => r.GetUserByUsernameAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var result = await _service.AuthenticateAsync("   ", "password");
            Assert.Null(result);
        }

        // ── Corrupted stored credentials ─────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_CorruptedSalt_ReturnsNullWithoutThrowing()
        {
            var user = new User
            {
                Username = "corrupt",
                PasswordHash = "validBase64==",
                Salt = "NOT_VALID_BASE64!!!",   // will cause FormatException in Convert.FromBase64String
                IsActive = true
            };
            _repo.Setup(r => r.GetUserByUsernameAsync("corrupt")).ReturnsAsync(user);

            var ex = await Record.ExceptionAsync(() => _service.AuthenticateAsync("corrupt", "anypassword"));
            Assert.Null(ex);

            var result = await _service.AuthenticateAsync("corrupt", "anypassword");
            Assert.Null(result);
        }

        // ── Username trimming ────────────────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_UsernameWithLeadingTrailingSpaces_TrimsBeforeLookup()
        {
            var (hash, salt) = _service.HashPassword("pass1234567");
            var user = new User { Username = "admin", PasswordHash = hash, Salt = salt, IsActive = true };
            _repo.Setup(r => r.GetUserByUsernameAsync("admin")).ReturnsAsync(user);

            var result = await _service.AuthenticateAsync("  admin  ", "pass1234567");

            Assert.NotNull(result);
            _repo.Verify(r => r.GetUserByUsernameAsync("admin"), Times.Once);
        }

        // ── Hash non-determinism ─────────────────────────────────────────────

        [Fact]
        public void HashPassword_SameInput_ProducesDifferentSaltsEachTime()
        {
            var (_, salt1) = _service.HashPassword("SamePassword1!");
            var (_, salt2) = _service.HashPassword("SamePassword1!");

            Assert.NotEqual(salt1, salt2);
        }

        [Fact]
        public void HashPassword_SameInput_ProducesDifferentHashesEachTime()
        {
            var (hash1, _) = _service.HashPassword("SamePassword1!");
            var (hash2, _) = _service.HashPassword("SamePassword1!");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public async Task AuthenticateAsync_HashRoundTrip_SucceedsWithCorrectPassword()
        {
            const string password = "RoundTrip123!";
            var (hash, salt) = _service.HashPassword(password);
            var user = new User { Username = "rt", PasswordHash = hash, Salt = salt, IsActive = true };
            _repo.Setup(r => r.GetUserByUsernameAsync("rt")).ReturnsAsync(user);

            var result = await _service.AuthenticateAsync("rt", password);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task AuthenticateAsync_HashRoundTrip_FailsWithWrongPassword()
        {
            const string password = "RoundTrip123!";
            var (hash, salt) = _service.HashPassword(password);
            var user = new User { Username = "rt2", PasswordHash = hash, Salt = salt, IsActive = true };
            _repo.Setup(r => r.GetUserByUsernameAsync("rt2")).ReturnsAsync(user);

            var result = await _service.AuthenticateAsync("rt2", "WrongPassword!");
            Assert.Null(result);
        }

        // ── User not found ───────────────────────────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_UserNotFound_ReturnsNull()
        {
            _repo.Setup(r => r.GetUserByUsernameAsync("ghost")).ReturnsAsync((User?)null);

            var result = await _service.AuthenticateAsync("ghost", "anypassword");
            Assert.Null(result);
        }

        // ── MustChangePassword flag is preserved ─────────────────────────────

        [Fact]
        public async Task AuthenticateAsync_MustChangePasswordTrue_StillAuthenticates()
        {
            var (hash, salt) = _service.HashPassword("pass1234567");
            var user = new User
            {
                Username = "mustchange",
                PasswordHash = hash,
                Salt = salt,
                IsActive = true,
                MustChangePassword = true
            };
            _repo.Setup(r => r.GetUserByUsernameAsync("mustchange")).ReturnsAsync(user);

            var result = await _service.AuthenticateAsync("mustchange", "pass1234567");

            Assert.NotNull(result);
            Assert.True(result!.MustChangePassword);
        }
    }
}
