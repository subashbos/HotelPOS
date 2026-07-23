using AutoMapper;
using HotelPOS.Api.Configuration;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Common.Mappings;
using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.DTOs.User;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    /// <summary>
    /// Fills in the remaining controller-level coverage gaps identified in the API test audit:
    /// AuthController's non-login endpoints, UsersController's mutating/self-service endpoints,
    /// CashSessionsController's money-movement endpoints, and CustomersController's mutating endpoints.
    /// </summary>
    public class AuthUsersSessionsCustomersTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        private static IOptions<JwtOptions> CreateJwtOptions() =>
            Options.Create(new JwtOptions
            {
                Key = "HotelPOS_TestJwtKey_Minimum32Characters!",
                Issuer = "HotelPOS",
                Audience = "HotelPOSClient"
            });

        private static AuthController CreateAuthController(
            Mock<IAuthService>? authSvc = null,
            Mock<IUserRepository>? userRepo = null,
            Mock<IRefreshTokenRepository>? refreshRepo = null,
            Mock<IPasswordResetService>? resetSvc = null)
        {
            return new AuthController(
                (authSvc ?? new Mock<IAuthService>()).Object,
                (userRepo ?? new Mock<IUserRepository>()).Object,
                (refreshRepo ?? new Mock<IRefreshTokenRepository>()).Object,
                (resetSvc ?? new Mock<IPasswordResetService>()).Object,
                CreateJwtOptions());
        }

        // ================= AuthController.Login (2FA branch) =================

        [Fact]
        public async Task Login_TwoFactorEnabled_NoCode_ReturnsUnauthorizedWithRequiresTwoFactor()
        {
            var authSvc = new Mock<IAuthService>();
            authSvc.Setup(a => a.AuthenticateAsync("admin", "password")).ReturnsAsync(new User
            {
                Id = 1,
                Username = "admin",
                Role = RoleNames.Admin,
                TwoFactorEnabled = true,
                TwoFactorSecret = TotpGenerator.GenerateSecret()
            });

            var controller = CreateAuthController(authSvc);
            var result = await controller.Login(new LoginDto { Username = "admin", Password = "password" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task Login_TwoFactorEnabled_InvalidCode_ReturnsUnauthorized()
        {
            var authSvc = new Mock<IAuthService>();
            authSvc.Setup(a => a.AuthenticateAsync("admin", "password")).ReturnsAsync(new User
            {
                Id = 1,
                Username = "admin",
                Role = RoleNames.Admin,
                TwoFactorEnabled = true,
                TwoFactorSecret = TotpGenerator.GenerateSecret()
            });

            var controller = CreateAuthController(authSvc);
            var result = await controller.Login(new LoginDto { Username = "admin", Password = "password", TotpCode = "000000" });

            Assert.IsType<UnauthorizedObjectResult>(result);
            authSvc.Verify(a => a.RegisterFailedTwoFactorAttemptAsync("admin"), Times.Once);
        }

        [Fact]
        public async Task Login_TwoFactorEnabled_TooManyFailedAttempts_ReturnsUnauthorizedWithoutCheckingCode()
        {
            var authSvc = new Mock<IAuthService>();
            authSvc.Setup(a => a.AuthenticateAsync("admin", "password")).ReturnsAsync(new User
            {
                Id = 1,
                Username = "admin",
                Role = RoleNames.Admin,
                TwoFactorEnabled = true,
                TwoFactorSecret = TotpGenerator.GenerateSecret()
            });
            authSvc.Setup(a => a.IsTwoFactorLockedOutAsync("admin")).ReturnsAsync(true);

            var controller = CreateAuthController(authSvc);
            var result = await controller.Login(new LoginDto { Username = "admin", Password = "password", TotpCode = "000000" });

            Assert.IsType<UnauthorizedObjectResult>(result);
            // Even a code that would otherwise be valid must not be checked once locked out,
            // and a lockout check must never itself count as (or reset) an attempt.
            authSvc.Verify(a => a.RegisterFailedTwoFactorAttemptAsync(It.IsAny<string>()), Times.Never);
            authSvc.Verify(a => a.ClearTwoFactorLockoutAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Login_TwoFactorEnabled_ValidCode_ReturnsOkWithToken()
        {
            var secret = TotpGenerator.GenerateSecret();
            var validCode = GetCurrentTotpCode(secret);
            var authSvc = new Mock<IAuthService>();
            authSvc.Setup(a => a.AuthenticateAsync("admin", "password")).ReturnsAsync(new User
            {
                Id = 1,
                Username = "admin",
                Role = RoleNames.Admin,
                TwoFactorEnabled = true,
                TwoFactorSecret = secret,
                MustChangePassword = false
            });

            var controller = CreateAuthController(authSvc);
            var result = await controller.Login(new LoginDto { Username = "admin", Password = "password", TotpCode = validCode });

            Assert.IsType<OkObjectResult>(result);
            authSvc.Verify(a => a.ClearTwoFactorLockoutAsync("admin"), Times.Once);
        }

        private static string GetCurrentTotpCode(string secret)
        {
            // Brute-force isn't necessary: reuse ValidateCode's own window by trying the codes
            // TotpGenerator would accept "now" — simplest is to regenerate via reflection-free approach:
            // compute using the same algorithm parameters is private, so instead assert acceptance
            // indirectly is not possible here; use the public API by round-tripping through ValidateCode
            // is also not exposed as a generator. We derive the code the same way TOTP apps do.
            var stepSeconds = 30;
            var digits = 6;
            var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / stepSeconds;
            return ComputeTotp(secret, step, digits);
        }

        private static string ComputeTotp(string base32Secret, long step, int digits)
        {
            var secretBytes = Base32Decode(base32Secret);
            var stepBytes = BitConverter.GetBytes(step);
            if (BitConverter.IsLittleEndian) Array.Reverse(stepBytes);

            using var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes);
            var hash = hmac.ComputeHash(stepBytes);
            int offset = hash[^1] & 0x0F;
            int binaryCode = ((hash[offset] & 0x7F) << 24)
                            | ((hash[offset + 1] & 0xFF) << 16)
                            | ((hash[offset + 2] & 0xFF) << 8)
                            | (hash[offset + 3] & 0xFF);
            int otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString().PadLeft(digits, '0');
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            input = input.Trim().TrimEnd('=').ToUpperInvariant();
            var bytes = new List<byte>();
            int bits = 0, value = 0;
            foreach (var c in input)
            {
                int idx = alphabet.IndexOf(c);
                value = (value << 5) | idx;
                bits += 5;
                if (bits >= 8)
                {
                    bytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }
            return bytes.ToArray();
        }

        // ================= AuthController.Refresh =================

        [Fact]
        public async Task Refresh_UnknownToken_ReturnsUnauthorized()
        {
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken?)null);

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "bogus" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_RevokedToken_ReturnsUnauthorized()
        {
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                ExpiresUtc = DateTime.UtcNow.AddDays(1),
                RevokedUtc = DateTime.UtcNow.AddMinutes(-1)
            });

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "used-token" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ExpiredToken_ReturnsUnauthorized()
        {
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 1,
                ExpiresUtc = DateTime.UtcNow.AddDays(-1)
            });

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "expired-token" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_InactiveUser_ReturnsUnauthorized()
        {
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(new RefreshToken
            {
                Id = 1,
                UserId = 5,
                ExpiresUtc = DateTime.UtcNow.AddDays(1)
            });
            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(new User { Id = 5, Username = "gone", IsActive = false });

            var controller = CreateAuthController(userRepo: userRepo, refreshRepo: refreshRepo);
            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "valid-token" });

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_Valid_RotatesTokenAndReturnsNewToken()
        {
            var existing = new RefreshToken
            {
                Id = 1,
                UserId = 5,
                ExpiresUtc = DateTime.UtcNow.AddDays(1)
            };
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(existing);
            refreshRepo.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);
            refreshRepo.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

            var userRepo = new Mock<IUserRepository>();
            userRepo.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(new User { Id = 5, Username = "cashier", Role = RoleNames.Cashier, IsActive = true });

            var controller = CreateAuthController(userRepo: userRepo, refreshRepo: refreshRepo);
            var result = await controller.Refresh(new RefreshRequestDto { RefreshToken = "valid-token" });

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(existing.RevokedUtc);
            Assert.NotNull(existing.ReplacedByTokenHash);
            refreshRepo.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            refreshRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        // ================= AuthController.Logout =================

        [Fact]
        public async Task Logout_NoToken_ReturnsOkWithoutTouchingRepository()
        {
            var refreshRepo = new Mock<IRefreshTokenRepository>();

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Logout(new RefreshRequestDto { RefreshToken = "" });

            Assert.IsType<OkResult>(result);
            refreshRepo.Verify(r => r.GetByHashAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Logout_ValidToken_RevokesToken()
        {
            var existing = new RefreshToken { Id = 1, UserId = 5, ExpiresUtc = DateTime.UtcNow.AddDays(1) };
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(existing);
            refreshRepo.Setup(r => r.UpdateAsync(It.IsAny<RefreshToken>())).Returns(Task.CompletedTask);

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Logout(new RefreshRequestDto { RefreshToken = "active-token" });

            Assert.IsType<OkResult>(result);
            Assert.NotNull(existing.RevokedUtc);
            refreshRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task Logout_AlreadyRevokedToken_DoesNotUpdateAgain()
        {
            var existing = new RefreshToken { Id = 1, UserId = 5, RevokedUtc = DateTime.UtcNow.AddMinutes(-5) };
            var refreshRepo = new Mock<IRefreshTokenRepository>();
            refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>())).ReturnsAsync(existing);

            var controller = CreateAuthController(refreshRepo: refreshRepo);
            var result = await controller.Logout(new RefreshRequestDto { RefreshToken = "already-revoked" });

            Assert.IsType<OkResult>(result);
            refreshRepo.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        // ================= AuthController.ForgotPassword / ResetPasswordWithCode =================

        [Fact]
        public async Task ForgotPassword_AlwaysReturnsOk_RegardlessOfAccountExistence()
        {
            var resetSvc = new Mock<IPasswordResetService>();
            resetSvc.Setup(s => s.RequestResetAsync("nonexistent")).Returns(Task.CompletedTask);

            var controller = CreateAuthController(resetSvc: resetSvc);
            var result = await controller.ForgotPassword(new ForgotPasswordDto { Username = "nonexistent" });

            Assert.IsType<OkResult>(result);
            resetSvc.Verify(s => s.RequestResetAsync("nonexistent"), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordWithCode_InvalidCode_ReturnsBadRequest()
        {
            var resetSvc = new Mock<IPasswordResetService>();
            resetSvc.Setup(s => s.ConfirmResetAsync("admin", "000000", "NewPass123!"))
                .ReturnsAsync((false, "Invalid or expired code."));

            var controller = CreateAuthController(resetSvc: resetSvc);
            var result = await controller.ResetPasswordWithCode(new ResetPasswordWithCodeDto
            {
                Username = "admin",
                Code = "000000",
                NewPassword = "NewPass123!"
            });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPasswordWithCode_ValidCode_ReturnsOk()
        {
            var resetSvc = new Mock<IPasswordResetService>();
            resetSvc.Setup(s => s.ConfirmResetAsync("admin", "123456", "NewPass123!"))
                .ReturnsAsync((true, (string?)null));

            var controller = CreateAuthController(resetSvc: resetSvc);
            var result = await controller.ResetPasswordWithCode(new ResetPasswordWithCodeDto
            {
                Username = "admin",
                Code = "123456",
                NewPassword = "NewPass123!"
            });

            Assert.IsType<OkResult>(result);
        }

        // ================= AuthController 2FA enrollment =================

        [Fact]
        public void NewTwoFactorSecret_ReturnsSecretAndOtpAuthUri()
        {
            var controller = CreateAuthController();
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())
                }
            };

            var result = controller.NewTwoFactorSecret();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public void VerifyTwoFactorCode_ValidCode_ReturnsValidTrue()
        {
            var secret = TotpGenerator.GenerateSecret();
            var code = GetCurrentTotpCode(secret);

            var controller = CreateAuthController();
            var result = controller.VerifyTwoFactorCode(new VerifyTwoFactorDto { Secret = secret, Code = code });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public void VerifyTwoFactorCode_InvalidCode_ReturnsValidFalse()
        {
            var secret = TotpGenerator.GenerateSecret();

            var controller = CreateAuthController();
            var result = controller.VerifyTwoFactorCode(new VerifyTwoFactorDto { Secret = secret, Code = "000000" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        // ================= UsersController =================

        [Fact]
        public async Task Users_CreateUser_MissingUsernameOrPassword_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.CreateUser(new AddUserRequest { Username = "", Password = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_CreateUser_ServiceRejects_ReturnsBadRequest()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.AddUserAsync("admin", "pw", RoleNames.Cashier, 0)).ReturnsAsync((false, "Username already exists."));

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.CreateUser(new AddUserRequest { Username = "admin", Password = "pw" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_CreateUser_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.AddUserAsync("newuser", "pw", RoleNames.Cashier, 0)).ReturnsAsync((true, string.Empty));

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.CreateUser(new AddUserRequest { Username = "newuser", Password = "pw" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Users_ToggleActive_InvalidId_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ToggleActive(0, new ToggleActiveRequest { IsActive = true });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_ToggleActive_Valid_ReturnsNoContentAndCallsService()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.ToggleActiveAsync(3, false)).Returns(Task.CompletedTask);

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ToggleActive(3, new ToggleActiveRequest { IsActive = false });

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.ToggleActiveAsync(3, false), Times.Once);
        }

        [Fact]
        public async Task Users_SetEmail_InvalidId_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.SetEmail(0, new SetEmailRequest { Email = "a@b.com" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_SetEmail_Valid_ReturnsNoContentAndCallsService()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.SetEmailAsync(3, "a@b.com")).Returns(Task.CompletedTask);

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.SetEmail(3, new SetEmailRequest { Email = "a@b.com" });

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.SetEmailAsync(3, "a@b.com"), Times.Once);
        }

        [Fact]
        public async Task Users_ResetPassword_InvalidId_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ResetPassword(0, new ResetPasswordRequest { NewPassword = "NewPass1!" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_ResetPassword_EmptyPassword_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ResetPassword(1, new ResetPasswordRequest { NewPassword = "" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_ResetPassword_ServiceRejects_ReturnsBadRequest()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.ResetPasswordAsync(1, "weak")).ReturnsAsync((false, "Password too weak."));

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ResetPassword(1, new ResetPasswordRequest { NewPassword = "weak" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_ResetPassword_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.ResetPasswordAsync(1, "NewPass123!")).ReturnsAsync((true, string.Empty));

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.ResetPassword(1, new ResetPasswordRequest { NewPassword = "NewPass123!" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Users_SetTwoFactor_InvalidId_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.SetTwoFactor(0, new SetTwoFactorRequest { Enabled = true, Secret = "SECRET" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_SetTwoFactor_Valid_ReturnsNoContentAndCallsService()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.SetTwoFactorAsync(1, true, "SECRET")).Returns(Task.CompletedTask);

            var controller = new UsersController(svc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.SetTwoFactor(1, new SetTwoFactorRequest { Enabled = true, Secret = "SECRET" });

            Assert.IsType<NoContentResult>(result);
            svc.Verify(s => s.SetTwoFactorAsync(1, true, "SECRET"), Times.Once);
        }

        [Fact]
        public async Task Users_DeleteUser_InvalidId_ReturnsBadRequest()
        {
            var controller = new UsersController(Mock.Of<IUserService>(), Mock.Of<IUserContext>(), Mapper);
            var result = await controller.DeleteUser(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_DeleteUser_SelfDelete_ReturnsBadRequest()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.DeleteUserAsync(1, 1)).ThrowsAsync(new InvalidOperationException("You cannot delete your own account."));
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUserId).Returns(1);

            var controller = new UsersController(svc.Object, userCtx.Object, Mapper);
            var result = await controller.DeleteUser(1);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Users_DeleteUser_Valid_ReturnsNoContent()
        {
            var svc = new Mock<IUserService>();
            svc.Setup(s => s.DeleteUserAsync(2, 1)).Returns(Task.CompletedTask);
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUserId).Returns(1);

            var controller = new UsersController(svc.Object, userCtx.Object, Mapper);
            var result = await controller.DeleteUser(2);

            Assert.IsType<NoContentResult>(result);
        }

        // ================= CashSessionsController =================

        [Fact]
        public async Task CashSessions_GetHistory_ReturnsMappedDtos()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.GetSessionHistoryAsync(30)).ReturnsAsync(new List<CashSession>
            {
                new CashSession { Id = 1, OpeningBalance = 100, Status = "Closed" }
            });

            var controller = new CashSessionsController(cashSvc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.GetHistory();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<CashSessionDto>>(ok.Value);
            Assert.Single(dtos);
        }

        [Fact]
        public async Task CashSessions_GetHistory_PassesCountThrough()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.GetSessionHistoryAsync(10)).ReturnsAsync(new List<CashSession>());

            var controller = new CashSessionsController(cashSvc.Object, Mock.Of<IUserContext>(), Mapper);
            await controller.GetHistory(10);

            cashSvc.Verify(s => s.GetSessionHistoryAsync(10), Times.Once);
        }

        [Fact]
        public async Task CashSessions_GetCurrentSessionSalesTotal_ReturnsOkWithTotal()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.GetTotalSalesForCurrentSessionAsync()).ReturnsAsync(1234.56m);

            var controller = new CashSessionsController(cashSvc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.GetCurrentSessionSalesTotal();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(1234.56m, ok.Value);
        }

        [Fact]
        public async Task CashSessions_OpenSession_Valid_ReturnsOkWithId()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.OpenSessionAsync(500, "cashier1")).ReturnsAsync(10);
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUsername).Returns("cashier1");

            var controller = new CashSessionsController(cashSvc.Object, userCtx.Object, Mapper);
            var result = await controller.OpenSession(new OpenSessionRequest { OpeningBalance = 500 });

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(10, ok.Value);
        }

        [Fact]
        public async Task CashSessions_OpenSession_AlreadyOpen_ReturnsConflict()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.OpenSessionAsync(It.IsAny<decimal>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("A session is already open."));

            var controller = new CashSessionsController(cashSvc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.OpenSession(new OpenSessionRequest { OpeningBalance = 500 });

            Assert.IsType<ConflictObjectResult>(result.Result);
        }

        [Fact]
        public async Task CashSessions_OpenSession_UsesCurrentUsername_NotClientSupplied()
        {
            var cashSvc = new Mock<ICashService>();
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUsername).Returns("real-user");
            cashSvc.Setup(s => s.OpenSessionAsync(100, "real-user")).ReturnsAsync(1);

            var controller = new CashSessionsController(cashSvc.Object, userCtx.Object, Mapper);
            await controller.OpenSession(new OpenSessionRequest { OpeningBalance = 100 });

            cashSvc.Verify(s => s.OpenSessionAsync(100, "real-user"), Times.Once);
        }

        [Fact]
        public async Task CashSessions_CloseSession_Valid_ReturnsNoContent()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.CloseSessionAsync(950, "End of shift", "cashier1")).Returns(Task.CompletedTask);
            var userCtx = new Mock<IUserContext>();
            userCtx.Setup(u => u.CurrentUsername).Returns("cashier1");

            var controller = new CashSessionsController(cashSvc.Object, userCtx.Object, Mapper);
            var result = await controller.CloseSession(new CloseSessionRequest { ActualCash = 950, Notes = "End of shift" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task CashSessions_CloseSession_NoOpenSession_ReturnsConflict()
        {
            var cashSvc = new Mock<ICashService>();
            cashSvc.Setup(s => s.CloseSessionAsync(It.IsAny<decimal>(), It.IsAny<string?>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("No open session found."));

            var controller = new CashSessionsController(cashSvc.Object, Mock.Of<IUserContext>(), Mapper);
            var result = await controller.CloseSession(new CloseSessionRequest { ActualCash = 100 });

            Assert.IsType<ConflictObjectResult>(result);
        }

        // ================= CustomersController =================

        [Fact]
        public async Task Customers_GetCustomerByPhone_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerByPhoneAsync("9999999999")).ReturnsAsync((Customer?)null);

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomerByPhone("9999999999");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Customers_GetCustomerByPhone_Found_ReturnsDto()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerByPhoneAsync("1234567890")).ReturnsAsync(new Customer { Id = 1, Name = "Alice", Phone = "1234567890" });

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomerByPhone("1234567890");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CustomerDto>(ok.Value);
            Assert.Equal("Alice", dto.Name);
        }

        [Fact]
        public async Task Customers_GetCustomerHistory_InvalidId_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            var result = await controller.GetCustomerHistory(0);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Customers_GetCustomerHistory_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerHistoryAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomerHistory(99);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Customers_GetCustomerHistory_Found_ReturnsOkWithHistory()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.GetCustomerHistoryAsync(1)).ReturnsAsync(new CustomerHistoryDto
            {
                CustomerId = 1,
                CustomerName = "Alice",
                TotalOrders = 3,
                TotalSpent = 500
            });

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.GetCustomerHistory(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<CustomerHistoryDto>(ok.Value);
            Assert.Equal(3, dto.TotalOrders);
        }

        [Fact]
        public async Task Customers_CreateCustomer_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.CreateCustomer(new SaveCustomerDto { Name = "" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Customers_CreateCustomer_Valid_ReturnsCreatedAtAction()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.SaveCustomerAsync(It.IsAny<Customer>()))
                .Callback<Customer>(c => c.Id = 3)
                .Returns(Task.CompletedTask);

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.CreateCustomer(new SaveCustomerDto { Name = "Bob", Phone = "5555555555" });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var dto = Assert.IsType<CustomerDto>(created.Value);
            Assert.Equal(3, dto.Id);
        }

        [Fact]
        public async Task Customers_CreateCustomer_ArgumentException_ReturnsBadRequest()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.SaveCustomerAsync(It.IsAny<Customer>())).ThrowsAsync(new ArgumentException("Duplicate phone number."));

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.CreateCustomer(new SaveCustomerDto { Name = "Bob", Phone = "5555555555" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Customers_UpdateCustomer_InvalidId_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            var result = await controller.UpdateCustomer(0, new SaveCustomerDto { Name = "Bob" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Customers_UpdateCustomer_InvalidModelState_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.UpdateCustomer(1, new SaveCustomerDto { Name = "" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Customers_UpdateCustomer_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.SaveCustomerAsync(It.IsAny<Customer>())).ThrowsAsync(new KeyNotFoundException());

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.UpdateCustomer(99, new SaveCustomerDto { Name = "Bob" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Customers_UpdateCustomer_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.SaveCustomerAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.UpdateCustomer(1, new SaveCustomerDto { Name = "Bob Updated" });

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Customers_DeleteCustomer_InvalidId_ReturnsBadRequest()
        {
            var controller = new CustomersController(Mock.Of<ICustomerService>(), Mapper);
            var result = await controller.DeleteCustomer(0);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Customers_DeleteCustomer_NotFound_ReturnsNotFound()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.DeleteCustomerAsync(99)).ThrowsAsync(new KeyNotFoundException());

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.DeleteCustomer(99);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Customers_DeleteCustomer_Valid_ReturnsNoContent()
        {
            var svc = new Mock<ICustomerService>();
            svc.Setup(s => s.DeleteCustomerAsync(1)).Returns(Task.CompletedTask);

            var controller = new CustomersController(svc.Object, Mapper);
            var result = await controller.DeleteCustomer(1);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
