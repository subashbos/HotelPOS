using HotelPOS.Api;
using HotelPOS.Api.Middleware;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using Xunit;

namespace HotelPOS.Tests
{
    public class PermissionsPreloadMiddlewareTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IHttpContextAccessor> _accessorMock;
        private bool _nextCalled;
        private readonly RequestDelegate _next;
        private readonly PermissionsPreloadMiddleware _middleware;

        public PermissionsPreloadMiddlewareTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _accessorMock = new Mock<IHttpContextAccessor>();
            _next = _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            };
            _middleware = new PermissionsPreloadMiddleware(_next);
        }

        private ApiUserContext CreateUserContext(HttpContext context)
        {
            _accessorMock.Setup(a => a.HttpContext).Returns(context);
            return new ApiUserContext(_accessorMock.Object, _userRepoMock.Object);
        }

        [Fact]
        public async Task InvokeAsync_AuthenticatedRequest_PreloadsPermissions()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "jdoe") }, "TestAuth");
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            var role = new Role { Name = "Cashier", Permissions = new List<RolePermission> { new() { ModuleName = "Orders", CanAccess = true } } };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe"))
                .ReturnsAsync(new User { Username = "jdoe", RoleDetails = role });

            var userContext = CreateUserContext(context);

            await _middleware.InvokeAsync(context, userContext);

            _userRepoMock.Verify(r => r.GetUserByUsernameAsync("jdoe"), Times.Once);
            Assert.NotNull(userContext.Permissions);
            Assert.Single(userContext.Permissions!);
        }

        [Fact]
        public async Task InvokeAsync_AnonymousRequest_SkipsPermissionsLoad()
        {
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var userContext = CreateUserContext(context);

            await _middleware.InvokeAsync(context, userContext);

            _userRepoMock.Verify(r => r.GetUserByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_AuthenticatedRequest_CallsNext()
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "jdoe") }, "TestAuth");
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _userRepoMock.Setup(r => r.GetUserByUsernameAsync("jdoe")).ReturnsAsync((User?)null);
            var userContext = CreateUserContext(context);

            await _middleware.InvokeAsync(context, userContext);

            Assert.True(_nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_AnonymousRequest_StillCallsNext()
        {
            var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
            var userContext = CreateUserContext(context);

            await _middleware.InvokeAsync(context, userContext);

            Assert.True(_nextCalled);
        }
    }
}
