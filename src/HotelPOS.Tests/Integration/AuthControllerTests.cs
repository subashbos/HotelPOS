using HotelPOS.Api.Controllers;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class AuthControllerTests
    {
        private static IConfiguration CreateConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "HotelPOS_TestJwtKey_Minimum32Characters!",
                    ["Jwt:Issuer"] = "HotelPOS",
                    ["Jwt:Audience"] = "HotelPOSClient"
                })
                .Build();

        [Fact]
        public async Task Login_WhenMustChangePassword_ReturnsUnauthorizedWithoutToken()
        {
            var authMock = new Mock<IAuthService>();
            authMock.Setup(a => a.AuthenticateAsync("admin", "password"))
                .ReturnsAsync(new User
                {
                    Id = 1,
                    Username = "admin",
                    Role = "Admin",
                    MustChangePassword = true
                });

            var controller = new AuthController(authMock.Object, CreateConfig());
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
                    Role = "Cashier",
                    MustChangePassword = false
                });

            var controller = new AuthController(authMock.Object, CreateConfig());
            var result = await controller.Login(new LoginDto { Username = "cashier", Password = "password" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);
        }
    }
}

