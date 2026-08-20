using HotelPOS.Api;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace HotelPOS.Tests
{
    public class ApiUserContextTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IHttpContextAccessor> _accessorMock;

        public ApiUserContextTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _accessorMock = new Mock<IHttpContextAccessor>();
        }

        private ApiUserContext CreateContext(HttpContext? httpContext)
        {
            _accessorMock.Setup(a => a.HttpContext).Returns(httpContext);
            return new ApiUserContext(_accessorMock.Object, _userRepoMock.Object);
        }

        private static DefaultHttpContext CreateAuthenticatedHttpContext(string userId, string username, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        }

        [Fact]
        public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);

            Assert.True(context.IsAuthenticated);
        }

        [Fact]
        public void IsAuthenticated_WithNoHttpContext_ReturnsFalse()
        {
            var context = CreateContext(null);

            Assert.False(context.IsAuthenticated);
        }

        [Fact]
        public void CurrentUserId_ParsesSubClaim()
        {
            var httpContext = CreateAuthenticatedHttpContext("42", "jdoe", "Admin");
            var context = CreateContext(httpContext);

            Assert.Equal(42, context.CurrentUserId);
        }

        [Fact]
        public void CurrentUserId_WithNoHttpContext_ReturnsNull()
        {
            var context = CreateContext(null);

            Assert.Null(context.CurrentUserId);
        }

        [Fact]
        public void CurrentUserId_WithNonNumericSub_ReturnsNull()
        {
            var httpContext = CreateAuthenticatedHttpContext("not-a-number", "jdoe", "Admin");
            var context = CreateContext(httpContext);

            Assert.Null(context.CurrentUserId);
        }

        [Fact]
        public void CurrentUsername_ReturnsUniqueNameClaim()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);

            Assert.Equal("jdoe", context.CurrentUsername);
        }

        [Fact]
        public void CurrentUsername_FallsBackToIdentityName_WhenUniqueNameClaimMissing()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "fallback-name") }, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            var context = CreateContext(httpContext);

            Assert.Equal("fallback-name", context.CurrentUsername);
        }

        [Fact]
        public void CurrentUsername_WithNoHttpContext_ReturnsNull()
        {
            var context = CreateContext(null);

            Assert.Null(context.CurrentUsername);
        }

        [Fact]
        public void CurrentRole_ReturnsRoleClaim()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Manager");
            var context = CreateContext(httpContext);

            Assert.Equal("Manager", context.CurrentRole);
        }

        [Fact]
        public void CurrentRole_WithNoHttpContext_ReturnsNull()
        {
            var context = CreateContext(null);

            Assert.Null(context.CurrentRole);
        }

        [Fact]
        public async Task EnsurePermissionsLoadedAsync_LoadsPermissionsForKnownUser()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);
            var role = new Role { Name = "Admin", Permissions = new List<RolePermission> { new() { ModuleName = "Billing", CanAccess = true } } };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe")).ReturnsAsync(new User { Username = "jdoe", RoleDetails = role });

            await context.EnsurePermissionsLoadedAsync();

            Assert.NotNull(context.Permissions);
            Assert.Single(context.Permissions!);
            Assert.Equal("Billing", context.Permissions![0].ModuleName);
        }

        [Fact]
        public async Task EnsurePermissionsLoadedAsync_IsIdempotent_OnlyQueriesRepositoryOnce()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe")).ReturnsAsync(new User { Username = "jdoe" });

            await context.EnsurePermissionsLoadedAsync();
            await context.EnsurePermissionsLoadedAsync();

            _userRepoMock.Verify(r => r.GetUserByUsernameAsync("jdoe"), Times.Once);
        }

        [Fact]
        public async Task EnsurePermissionsLoadedAsync_WithNoUsername_DoesNotQueryRepository()
        {
            var context = CreateContext(null);

            await context.EnsurePermissionsLoadedAsync();

            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
            Assert.Null(context.Permissions);
        }

        [Fact]
        public void Permissions_WithoutPreload_LazilyLoadsSynchronously()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);
            var role = new Role { Name = "Admin", Permissions = new List<RolePermission> { new() { ModuleName = "Reports" } } };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe")).ReturnsAsync(new User { Username = "jdoe", RoleDetails = role });

            var permissions = context.Permissions;

            Assert.NotNull(permissions);
            Assert.Single(permissions!);
            _userRepoMock.Verify(r => r.GetUserByUsernameAsync("jdoe"), Times.Once);
        }

        [Fact]
        public void Permissions_UserNotFound_ReturnsNull()
        {
            var httpContext = CreateAuthenticatedHttpContext("5", "jdoe", "Admin");
            var context = CreateContext(httpContext);
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe")).ReturnsAsync((User?)null);

            Assert.Null(context.Permissions);
        }
    }
}
