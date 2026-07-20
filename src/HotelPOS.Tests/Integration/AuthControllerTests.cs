using HotelPOS.Domain.Common.Constants;
using HotelPOS.Api.Configuration;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace HotelPOS.Tests
{
    public class AuthControllerTests
    {
        private static IOptions<JwtOptions> CreateJwtOptions() =>
            Options.Create(new JwtOptions
            {
                Key = "HotelPOS_TestJwtKey_Minimum32Characters!",
                Issuer = "HotelPOS",
                Audience = "HotelPOSClient"
            });

        [Fact]
        public async Task Login_WhenMustChangePassword_ReturnsUnauthorizedWithoutToken()
        {
            var authMock = new Mock<IAuthService>();
            authMock.Setup(a => a.AuthenticateAsync("admin", "password"))
                .ReturnsAsync(new User
                {
                    Id = 1,
                    Username = "admin",
                    Role = RoleNames.Admin,
                    MustChangePassword = true
                });

            var controller = new AuthController(
                authMock.Object,
                new Mock<IUserRepository>().Object,
                new Mock<IRefreshTokenRepository>().Object,
                new Mock<IPasswordResetService>().Object,
                CreateJwtOptions());
            var result = await controller.Login(new LoginDto { Username = "admin", Password = "password" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task Login_WhenPasswordIsCurrent_ReturnsToken()
        {
            var authMock = new Mock<IAuthService>();
            authMock.Setup(a => a.AuthenticateAsync("cashier", "password"))
                .ReturnsAsync(new User
                {
                    Id = 2,
                    Username = "cashier",
                    Role = RoleNames.Cashier,
                    MustChangePassword = false
                });

            var controller = new AuthController(
                authMock.Object,
                new Mock<IUserRepository>().Object,
                new Mock<IRefreshTokenRepository>().Object,
                new Mock<IPasswordResetService>().Object,
                CreateJwtOptions());
            var result = await controller.Login(new LoginDto { Username = "cashier", Password = "password" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("admin", "")]
        [InlineData("", "")]
        public void LoginDto_MissingUsernameOrPassword_FailsDataAnnotationValidation(string username, string password)
        {
            // [ApiController]'s automatic model validation (which returns 400 before the action
            // runs) only applies during real HTTP model binding, not direct action-method calls,
            // so this exercises the DataAnnotations directly instead.
            var dto = new LoginDto { Username = username, Password = password };
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true);

            Assert.False(isValid);
        }

        [Fact]
        public void LoginDto_ValidUsernameAndPassword_PassesDataAnnotationValidation()
        {
            var dto = new LoginDto { Username = "admin", Password = "password" };
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), validationResults, validateAllProperties: true);

            Assert.True(isValid);
        }
    }
}

