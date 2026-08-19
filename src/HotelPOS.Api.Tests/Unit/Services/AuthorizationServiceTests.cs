using HotelPOS.Domain.Common.Constants;
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
            var service = CreateService(true, RoleNames.Admin);
            Assert.True(service.HasPermission("Settings"));
            Assert.True(service.HasPermission("Roles"));
        }

        [Fact]
        public void HasPermission_Cashier_ReturnsTrueOnlyForBillingAndShift()
        {
            var service = CreateService(true, RoleNames.Cashier);
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
            var service = CreateService(true, RoleNames.Admin, permissions);
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
            var service = CreateService(true, RoleNames.Cashier);
            Assert.Throws<UnauthorizedAccessException>(() => service.EnsurePermission("Settings"));
        }

        [Fact]
        public void HasEditPermission_CanAccessButNotCanEdit_ReturnsFalse()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = true, CanEdit = false, CanDelete = true }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            Assert.True(service.HasPermission("Items"));
            Assert.False(service.HasEditPermission("Items"));
        }

        [Fact]
        public void EnsureEditPermission_CanAccessButNotCanEdit_Throws()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = true, CanEdit = false, CanDelete = true }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            Assert.Throws<UnauthorizedAccessException>(() => service.EnsureEditPermission("Items"));
        }

        [Fact]
        public void HasDeletePermission_CanAccessButNotCanDelete_ReturnsFalse()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = true, CanEdit = true, CanDelete = false }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            Assert.True(service.HasPermission("Items"));
            Assert.True(service.HasEditPermission("Items"));
            Assert.False(service.HasDeletePermission("Items"));
        }

        [Fact]
        public void EnsureDeletePermission_CanAccessButNotCanDelete_Throws()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = true, CanEdit = true, CanDelete = false }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            Assert.Throws<UnauthorizedAccessException>(() => service.EnsureDeletePermission("Items"));
        }

        [Fact]
        public void EnsureEditAndDeletePermission_WhenAllGranted_DoesNotThrow()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = true, CanEdit = true, CanDelete = true }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            var editEx = Record.Exception(() => service.EnsureEditPermission("Items"));
            var deleteEx = Record.Exception(() => service.EnsureDeletePermission("Items"));

            Assert.Null(editEx);
            Assert.Null(deleteEx);
        }

        [Fact]
        public void HasEditPermission_NoCanAccess_ReturnsFalseEvenIfCanEditIsTrue()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Items", CanAccess = false, CanEdit = true, CanDelete = true }
            };
            var service = CreateService(true, RoleNames.Admin, permissions);

            Assert.False(service.HasEditPermission("Items"));
            Assert.False(service.HasDeletePermission("Items"));
        }

        [Fact]
        public void HasEditAndDeletePermission_NoExplicitPermissions_FallsBackToRoleName()
        {
            // Roles with no configured RolePermission rows use the same role-name fallback as
            // HasPermission - Admin gets everything, Cashier only Billing/Shift.
            var admin = CreateService(true, RoleNames.Admin, permissions: null);
            Assert.True(admin.HasEditPermission("Settings"));
            Assert.True(admin.HasDeletePermission("Settings"));

            var cashier = CreateService(true, RoleNames.Cashier, permissions: null);
            Assert.True(cashier.HasEditPermission("Billing"));
            Assert.False(cashier.HasEditPermission("Settings"));
            Assert.False(cashier.HasDeletePermission("Settings"));
        }
    }
}

