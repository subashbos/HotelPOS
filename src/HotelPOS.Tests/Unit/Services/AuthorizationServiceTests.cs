using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class AuthorizationServiceTests
    {
        private static AuthorizationService CreateService(
            bool isAuthenticated,
            string? role,
            IReadOnlyList<RolePermission>? permissions = null)
        {
            var context = new Mock<IUserContext>();
            context.Setup(c => c.IsAuthenticated).Returns(isAuthenticated);
            context.Setup(c => c.CurrentRole).Returns(role);
            context.Setup(c => c.Permissions).Returns(permissions);
            return new AuthorizationService(context.Object);
        }

        [Fact]
        public void HasPermission_Admin_ReturnsTrueForAnyModule()
        {
            var service = CreateService(true, "Admin");
            Assert.True(service.HasPermission("Settings"));
            Assert.True(service.HasPermission("Roles"));
        }

        [Fact]
        public void HasPermission_Cashier_ReturnsTrueOnlyForBillingAndShift()
        {
            var service = CreateService(true, "Cashier");
            Assert.True(service.HasPermission("Billing"));
            Assert.True(service.HasPermission("Shift"));
            Assert.False(service.HasPermission("Settings"));
        }

        [Fact]
        public void HasPermission_UsesRolePermissionsWhenPresent()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Settings", CanAccess = false }
            };
            var service = CreateService(true, "Admin", permissions);
            Assert.False(service.HasPermission("Settings"));
        }

        [Fact]
        public void EnsurePermission_WhenUnauthenticated_Throws()
        {
            var service = CreateService(false, null);
            Assert.Throws<UnauthorizedAccessException>(() => service.EnsurePermission("Billing"));
        }

        [Fact]
        public void EnsurePermission_WhenCashierAccessesSettings_Throws()
        {
            var service = CreateService(true, "Cashier");
            Assert.Throws<UnauthorizedAccessException>(() => service.EnsurePermission("Settings"));
        }
    }
}

