using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HotelPOS.Tests
{
    public class RoleServiceTests
    {
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly RoleService _service;

        public RoleServiceTests()
        {
            _roleRepoMock = new Mock<IRoleRepository>();
            _service = new RoleService(_roleRepoMock.Object, TestAuthorization.AllowAll().Object, isTest: true);
        }

        [Fact]
        public async Task AddRoleAsync_ShouldCreateRoleWithDefaultPermissions()
        {
            // Arrange
            string roleName = "NewRole";
            _roleRepoMock.Setup(r => r.GetRoleByNameAsync(roleName)).ReturnsAsync((Role?)null);
            Role? capturedRole = null;
            _roleRepoMock.Setup(r => r.AddRoleAsync(It.IsAny<Role>()))
                .Callback<Role>(r => capturedRole = r)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddRoleAsync(roleName, "Description");

            // Assert
            Assert.True(result);
            _roleRepoMock.Verify(r => r.AddRoleAsync(It.IsAny<Role>()), Times.Once);
            Assert.NotNull(capturedRole);
            Assert.Equal(roleName, capturedRole.Name);
            // Verify all 12 modules are present (including 'Roles' and 'SalesReport')
            Assert.Equal(12, capturedRole.Permissions.Count);
            Assert.All(capturedRole.Permissions, p => Assert.False(p.CanAccess));
        }

        [Fact]
        public async Task AddRoleAsync_ShouldReturnFalse_IfRoleExists()
        {
            // Arrange
            string roleName = "Admin";
            _roleRepoMock.Setup(r => r.GetRoleByNameAsync(roleName)).ReturnsAsync(new Role { Name = roleName });

            // Act
            var result = await _service.AddRoleAsync(roleName, "Desc");

            // Assert
            Assert.False(result);
            _roleRepoMock.Verify(r => r.AddRoleAsync(It.IsAny<Role>()), Times.Never);
        }

        [Fact]
        public async Task UpdateRolePermissionsAsync_ShouldCallRepository()
        {
            // Arrange
            int roleId = 1;
            var permissions = new List<RolePermission> { new RolePermission { ModuleName = "Dashboard", CanAccess = true } };

            // Act
            await _service.UpdateRolePermissionsAsync(roleId, permissions);

            // Assert
            _roleRepoMock.Verify(r => r.UpdatePermissionsAsync(roleId, permissions), Times.Once);
        }

        [Fact]
        public async Task GetAllRolesAsync_ShouldReturnRoles()
        {
            // Arrange
            var roles = new List<Role> { new Role { Name = "Admin" }, new Role { Name = "Cashier" } };
            _roleRepoMock.Setup(r => r.GetAllRolesAsync()).ReturnsAsync(roles);

            // Act
            var result = await _service.GetAllRolesAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Admin", result[0].Name);
        }

        [Fact]
        public async Task AddRoleAsync_ShouldThrowException_IfNameIsEmpty()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<System.ArgumentException>(() => _service.AddRoleAsync("  ", "Desc"));
            Assert.Contains("Role name cannot be empty.", ex.Message);
            _roleRepoMock.Verify(r => r.AddRoleAsync(It.IsAny<Role>()), Times.Never);
        }

        [Fact]
        public async Task GetRoleByIdAsync_ShouldReturnRole_FromRepository()
        {
            // Arrange
            int roleId = 1;
            var expectedRole = new Role { Id = roleId, Name = "Admin" };
            _roleRepoMock.Setup(r => r.GetRoleByIdAsync(roleId)).ReturnsAsync(expectedRole);

            // Act
            var result = await _service.GetRoleByIdAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId, result.Id);
            Assert.Equal("Admin", result.Name);
            _roleRepoMock.Verify(r => r.GetRoleByIdAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task DeleteRoleAsync_ShouldCallRepository_Delete()
        {
            // Arrange
            int roleId = 5;

            // Act
            await _service.DeleteRoleAsync(roleId);

            // Assert
            _roleRepoMock.Verify(r => r.DeleteRoleAsync(roleId), Times.Once);
        }
    }
}
