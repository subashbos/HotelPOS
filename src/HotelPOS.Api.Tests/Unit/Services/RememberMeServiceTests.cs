using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class RememberMeServiceTests
    {
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IRememberMeTokenRepository> _tokenRepo = new();
        private readonly RememberMeService _service;

        public RememberMeServiceTests()
        {
            _service = new RememberMeService(_userRepo.Object, _tokenRepo.Object);
        }

        private static string Sha256Base64(string value) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private static User ValidUser(int id = 5, string username = "cashier") => new()
        {
            Id = id,
            Username = username,
            IsActive = true,
            MustChangePassword = false,
            TwoFactorEnabled = false
        };

        private RememberMeToken SetupToken(string rawToken, Action<RememberMeToken>? mutate = null)
        {
            var token = new RememberMeToken
            {
                UserId = 5,
                TokenHash = Sha256Base64(rawToken),
                CreatedUtc = DateTime.UtcNow,
                ExpiresUtc = DateTime.UtcNow.AddDays(30)
            };
            mutate?.Invoke(token);
            _tokenRepo.Setup(r => r.GetByHashAsync(token.TokenHash)).ReturnsAsync(token);
            return token;
        }

        [Fact]
        public async Task IssueTokenAsync_StoresOnlyHash_ReturnsRawToken()
        {
            RememberMeToken? stored = null;
            _tokenRepo.Setup(r => r.AddAsync(It.IsAny<RememberMeToken>()))
                .Callback<RememberMeToken>(t => stored = t)
                .Returns(Task.CompletedTask);

            var raw = await _service.IssueTokenAsync(5);

            Assert.False(string.IsNullOrEmpty(raw));
            Assert.NotNull(stored);
            Assert.Equal(5, stored!.UserId);
            Assert.NotEqual(raw, stored.TokenHash);
            Assert.Equal(Sha256Base64(raw), stored.TokenHash);
            Assert.True(stored.ExpiresUtc > DateTime.UtcNow.AddDays(29));
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_EmptyToken_ReturnsNull()
        {
            Assert.Null(await _service.ValidateAndConsumeAsync("cashier", ""));
            _tokenRepo.Verify(r => r.GetByHashAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_UnknownToken_ReturnsNull()
        {
            _tokenRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((RememberMeToken?)null);

            Assert.Null(await _service.ValidateAndConsumeAsync("cashier", "no-such-token"));
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_RevokedToken_ReturnsNull()
        {
            SetupToken("raw", t => t.RevokedUtc = DateTime.UtcNow.AddMinutes(-5));

            Assert.Null(await _service.ValidateAndConsumeAsync("cashier", "raw"));
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_ExpiredToken_ReturnsNull()
        {
            SetupToken("raw", t => t.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1));

            Assert.Null(await _service.ValidateAndConsumeAsync("cashier", "raw"));
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_ValidToken_ReturnsUserAndRevokesToken()
        {
            var token = SetupToken("raw");
            _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(ValidUser());

            var user = await _service.ValidateAndConsumeAsync("CASHIER", "raw");

            Assert.NotNull(user);
            Assert.NotNull(token.RevokedUtc); // single-use
            _tokenRepo.Verify(r => r.UpdateAsync(token), Times.Once);
        }

        [Theory]
        [InlineData("inactive")]
        [InlineData("mustChange")]
        [InlineData("twoFactor")]
        [InlineData("wrongUsername")]
        public async Task ValidateAndConsumeAsync_IneligibleUser_ReturnsNullButStillRevokes(string scenario)
        {
            var token = SetupToken("raw");
            var user = ValidUser();
            switch (scenario)
            {
                case "inactive": user.IsActive = false; break;
                case "mustChange": user.MustChangePassword = true; break;
                case "twoFactor": user.TwoFactorEnabled = true; break;
                case "wrongUsername": user.Username = "someoneelse"; break;
            }
            _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);

            var result = await _service.ValidateAndConsumeAsync("cashier", "raw");

            Assert.Null(result);
            // Token burns even on failure so a replayed token is useless.
            Assert.NotNull(token.RevokedUtc);
            _tokenRepo.Verify(r => r.UpdateAsync(token), Times.Once);
        }

        [Fact]
        public async Task ValidateAndConsumeAsync_MissingUser_ReturnsNullButStillRevokes()
        {
            var token = SetupToken("raw");
            _userRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((User?)null);

            Assert.Null(await _service.ValidateAndConsumeAsync("cashier", "raw"));
            Assert.NotNull(token.RevokedUtc);
        }

        [Fact]
        public async Task RevokeAsync_EmptyToken_DoesNothing()
        {
            await _service.RevokeAsync("");
            _tokenRepo.Verify(r => r.GetByHashAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RevokeAsync_ActiveToken_MarksRevoked()
        {
            var token = SetupToken("raw");

            await _service.RevokeAsync("raw");

            Assert.NotNull(token.RevokedUtc);
            _tokenRepo.Verify(r => r.UpdateAsync(token), Times.Once);
        }

        [Fact]
        public async Task RevokeAsync_AlreadyRevoked_DoesNotUpdateAgain()
        {
            SetupToken("raw", t => t.RevokedUtc = DateTime.UtcNow.AddDays(-1));

            await _service.RevokeAsync("raw");

            _tokenRepo.Verify(r => r.UpdateAsync(It.IsAny<RememberMeToken>()), Times.Never);
        }
    }
}
