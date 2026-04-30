using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SecurityPolicyTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public SecurityPolicyTests()
        {
            _authService = new AuthService(_userRepo.Object);
            _userService = new UserService(_userRepo.Object);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsUserWithMustChangePasswordFlag()
        {
            var (hash, salt) = _authService.HashPassword("password123");
            var user = new User 
            { 
                Username = "testuser", 
                PasswordHash = hash, 
                Salt = salt, 
                IsActive = true,
                MustChangePassword = true 
            };

            _userRepo.Setup(r => r.GetUserByUsernameAsync("testuser")).ReturnsAsync(user);

            var result = await _authService.AuthenticateAsync("testuser", "password123");

            Assert.NotNull(result);
            Assert.True(result.MustChangePassword);
        }

        [Fact]
        public async Task ResetPasswordAsync_ClearsMustChangePasswordFlag()
        {
            var user = new User { Id = 1, Username = "testuser", MustChangePassword = true };
            _userRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

            var (ok, _) = await _userService.ResetPasswordAsync(1, "newpassword123");

            Assert.True(ok);
            Assert.False(user.MustChangePassword);
            _userRepo.Verify(r => r.UpdateAsync(It.Is<User>(u => u.MustChangePassword == false)), Times.Once);
        }
    }
}
