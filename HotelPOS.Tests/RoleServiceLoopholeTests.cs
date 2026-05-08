using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers RoleService edge cases missing from RoleServiceTests.cs:
    /// empty name validation, delete non-existent, GetById null,
    /// UpdatePermissions with empty list, and correct 11-module count.
    /// </summary>
    public class RoleServiceLoopholeTests
    {
        private readonly Mock<IRoleRepository> _repo = new();
        private readonly RoleService _service;

        public RoleServiceLoopholeTests()
        {
            _service = new RoleService(_repo.Object);
        }

        // ── AddRoleAsync name validation ─────────────────────────────────────

        [Fact]
        public async Task AddRoleAsync_EmptyName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddRoleAsync("", "desc"));
        }

        [Fact]
        public async Task AddRoleAsync_WhitespaceName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddRoleAsync("   ", "desc"));
        }

        [Fact]
        public async Task AddRoleAsync_NullName_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddRoleAsync(null!, "desc"));
        }

        [Fact]
        public async Task AddRoleAsync_TrimsName_BeforeSaving()
        {
            _repo.Setup(r => r.GetRoleByNameAsync("Manager")).ReturnsAsync((Role?)null);
            Role? captured = null;
            _repo.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                 .Callback<Role>(r => captured = r);

            await _service.AddRoleAsync("  Manager  ", "desc");

            Assert.Equal("Manager", captured!.Name);
        }

        // ── AddRoleAsync creates exactly 11 modules (including "Roles") ──────

        [Fact]
        public async Task AddRoleAsync_CreatesElevenDefaultPermissions()
        {
            _repo.Setup(r => r.GetRoleByNameAsync(It.IsAny<string>())).ReturnsAsync((Role?)null);
            Role? captured = null;
            _repo.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                 .Callback<Role>(r => captured = r);

            await _service.AddRoleAsync("Supervisor", "desc");

            Assert.Equal(11, captured!.Permissions.Count);
        }

        [Fact]
        public async Task AddRoleAsync_DefaultPermissions_IncludesRolesModule()
        {
            _repo.Setup(r => r.GetRoleByNameAsync(It.IsAny<string>())).ReturnsAsync((Role?)null);
            Role? captured = null;
            _repo.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                 .Callback<Role>(r => captured = r);

            await _service.AddRoleAsync("Supervisor", "desc");

            Assert.Contains(captured!.Permissions, p => p.ModuleName == "Roles");
        }

        [Fact]
        public async Task AddRoleAsync_DefaultPermissions_AllModulesPresent()
        {
            _repo.Setup(r => r.GetRoleByNameAsync(It.IsAny<string>())).ReturnsAsync((Role?)null);
            Role? captured = null;
            _repo.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                 .Callback<Role>(r => captured = r);

            await _service.AddRoleAsync("Supervisor", "desc");

            var expected = new[] { "Dashboard", "Billing", "Items", "Categories", "Tables", "Ledger", "Journal", "Settings", "Audit", "Shift", "Roles" };
            foreach (var module in expected)
                Assert.Contains(captured!.Permissions, p => p.ModuleName == module);
        }

        [Fact]
        public async Task AddRoleAsync_AllDefaultPermissions_AreFalse()
        {
            _repo.Setup(r => r.GetRoleByNameAsync(It.IsAny<string>())).ReturnsAsync((Role?)null);
            Role? captured = null;
            _repo.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                 .Callback<Role>(r => captured = r);

            await _service.AddRoleAsync("Supervisor", "desc");

            Assert.All(captured!.Permissions, p => Assert.False(p.CanAccess));
        }

        // ── DeleteRoleAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task DeleteRoleAsync_CallsRepository()
        {
            await _service.DeleteRoleAsync(5);

            _repo.Verify(r => r.DeleteRoleAsync(5), Times.Once);
        }

        [Fact]
        public async Task DeleteRoleAsync_NonExistentId_DoesNotThrow()
        {
            _repo.Setup(r => r.DeleteRoleAsync(999)).Returns(Task.CompletedTask);

            var ex = await Record.ExceptionAsync(() => _service.DeleteRoleAsync(999));
            Assert.Null(ex);
        }

        // ── GetRoleByIdAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetRoleByIdAsync_UnknownId_ReturnsNull()
        {
            _repo.Setup(r => r.GetRoleByIdAsync(999)).ReturnsAsync((Role?)null);

            var result = await _service.GetRoleByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetRoleByIdAsync_KnownId_ReturnsRole()
        {
            var role = new Role { Id = 1, Name = "Admin" };
            _repo.Setup(r => r.GetRoleByIdAsync(1)).ReturnsAsync(role);

            var result = await _service.GetRoleByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Admin", result!.Name);
        }

        // ── UpdateRolePermissionsAsync ───────────────────────────────────────

        [Fact]
        public async Task UpdateRolePermissionsAsync_EmptyList_CallsRepository()
        {
            await _service.UpdateRolePermissionsAsync(1, new List<RolePermission>());

            _repo.Verify(r => r.UpdatePermissionsAsync(1, It.Is<List<RolePermission>>(l => l.Count == 0)), Times.Once);
        }

        [Fact]
        public async Task UpdateRolePermissionsAsync_PassesPermissionsToRepo()
        {
            var perms = new List<RolePermission>
            {
                new RolePermission { ModuleName = "Billing", CanAccess = true },
                new RolePermission { ModuleName = "Dashboard", CanAccess = false }
            };

            await _service.UpdateRolePermissionsAsync(2, perms);

            _repo.Verify(r => r.UpdatePermissionsAsync(2, perms), Times.Once);
        }
    }
}
