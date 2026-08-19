using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Entities;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class PasswordResetServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IPasswordResetRepository> _resetRepo = new();
        private readonly Mock<IEmailService> _email = new();
        private readonly PasswordResetService _service;

        public PasswordResetServiceTests()
        {
            _service = new PasswordResetService(_userRepo.Object, _resetRepo.Object, _email.Object);
        }

        private static User ActiveUser() => new()
        {
            Id = 7,
            Username = "cashier",
            Email = "cashier@example.com",
            IsActive = true,
            PasswordHash = "old-hash",
            Salt = "old-salt"
        };

        private static string Sha256Base64(string value) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        [Fact]
        public async Task RequestResetAsync_BlankUsername_DoesNothing()
        {
            await _service.RequestResetAsync("   ");

            _userRepo.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
            _resetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Never);
        }

        [Fact]
        public async Task RequestResetAsync_UnknownUser_DoesNotRevealOrSend()
        {
            _userRepo.Setup(r => r.GetUserByUsernameAsync("ghost")).ReturnsAsync((User?)null);

            await _service.RequestResetAsync("ghost");

            _resetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Never);
            _email.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RequestResetAsync_InactiveUser_DoesNotSend()
        {
            var user = ActiveUser();
            user.IsActive = false;
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);

            await _service.RequestResetAsync(user.Username);

            _resetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Never);
        }

        [Fact]
        public async Task RequestResetAsync_UserWithoutEmail_DoesNotSend()
        {
            var user = ActiveUser();
            user.Email = "  ";
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);

            await _service.RequestResetAsync(user.Username);

            _resetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Never);
        }

        [Fact]
        public async Task RequestResetAsync_ValidUser_StoresHashedCodeAndEmailsIt()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);

            PasswordResetRequest? stored = null;
            _resetRepo.Setup(r => r.AddAsync(It.IsAny<PasswordResetRequest>()))
                .Callback<PasswordResetRequest>(r => stored = r)
                .Returns(Task.CompletedTask);

            string? emailBody = null;
            _email.Setup(e => e.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, body) => emailBody = body)
                .Returns(Task.CompletedTask);

            await _service.RequestResetAsync($"  {user.Username}  ");

            Assert.NotNull(stored);
            Assert.Equal(user.Id, stored!.UserId);
            Assert.True(stored.ExpiresUtc > DateTime.UtcNow.AddMinutes(14));
            Assert.NotNull(emailBody);

            // The emailed 6-digit code must hash to what was persisted (never stored in plain text).
            var code = new string(emailBody!.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
            Assert.Equal(6, code.Length);
            Assert.Equal(Sha256Base64(code), stored.CodeHash);
        }

        [Fact]
        public async Task RequestResetAsync_EmailFailure_IsSwallowed()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _email.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP down"));

            // Must not propagate SMTP failures to the unauthenticated caller.
            await _service.RequestResetAsync(user.Username);

            _resetRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetRequest>()), Times.Once);
        }

        [Fact]
        public async Task ConfirmResetAsync_UnknownUser_Fails()
        {
            _userRepo.Setup(r => r.GetUserByUsernameAsync("ghost")).ReturnsAsync((User?)null);

            var (success, error) = await _service.ConfirmResetAsync("ghost", "123456", "N3wPassword!x");

            Assert.False(success);
            Assert.NotNull(error);
        }

        [Fact]
        public async Task ConfirmResetAsync_NoActiveRequest_Fails()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync((PasswordResetRequest?)null);

            var (success, _) = await _service.ConfirmResetAsync(user.Username, "123456", "N3wPassword!x");

            Assert.False(success);
        }

        [Fact]
        public async Task ConfirmResetAsync_ExpiredRequest_Fails()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(-1)
            });

            var (success, _) = await _service.ConfirmResetAsync(user.Username, "123456", "N3wPassword!x");

            Assert.False(success);
        }

        [Fact]
        public async Task ConfirmResetAsync_WrongCode_Fails()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
            });

            var (success, _) = await _service.ConfirmResetAsync(user.Username, "654321", "N3wPassword!x");

            Assert.False(success);
            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmResetAsync_WrongCode_IncrementsAttemptCount()
        {
            var user = ActiveUser();
            var request = new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = 2
            };
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(request);

            var (success, _) = await _service.ConfirmResetAsync(user.Username, "654321", "N3wPassword!x");

            Assert.False(success);
            Assert.Equal(3, request.AttemptCount);
            _resetRepo.Verify(r => r.UpdateAsync(request), Times.Once);
        }

        [Fact]
        public async Task ConfirmResetAsync_AttemptCountAtMax_FailsAndBurnsCodeEvenWithCorrectCode()
        {
            // Brute-forcing the 6-digit code must be capped: once the max wrong guesses is
            // reached, the code is invalidated outright — even the *correct* code no longer works.
            var user = ActiveUser();
            var request = new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(10),
                AttemptCount = HotelPOS.Domain.Common.Constants.SecurityDefaults.MaxPasswordResetCodeAttempts
            };
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(request);

            var (success, _) = await _service.ConfirmResetAsync(user.Username, "123456", "N3wPassword!x");

            Assert.False(success);
            Assert.True(request.Used);
            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmResetAsync_WeakNewPassword_FailsWithPolicyMessage()
        {
            var user = ActiveUser();
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
            });

            var (success, error) = await _service.ConfirmResetAsync(user.Username, "123456", "short");

            Assert.False(success);
            Assert.Equal(PasswordPolicy.RequirementsMessage, error);
            _userRepo.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmResetAsync_ValidCodeAndPassword_UpdatesUserAndConsumesRequest()
        {
            var user = ActiveUser();
            user.MustChangePassword = true;
            var request = new PasswordResetRequest
            {
                UserId = user.Id,
                CodeHash = Sha256Base64("123456"),
                ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
            };
            _userRepo.Setup(r => r.GetUserByUsernameAsync(user.Username)).ReturnsAsync(user);
            _resetRepo.Setup(r => r.GetLatestActiveAsync(user.Id)).ReturnsAsync(request);

            var (success, error) = await _service.ConfirmResetAsync(user.Username, " 123456 ", "N3wPassword!x");

            Assert.True(success);
            Assert.Null(error);
            Assert.NotEqual("old-hash", user.PasswordHash);
            Assert.NotEqual("old-salt", user.Salt);
            Assert.False(user.MustChangePassword);
            Assert.True(request.Used);
            _userRepo.Verify(r => r.UpdateAsync(user), Times.Once);
            _resetRepo.Verify(r => r.UpdateAsync(request), Times.Once);
        }
    }
}
