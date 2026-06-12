using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAuthorizationService _authorization;

        public RoleService(IRoleRepository roleRepository, IAuthorizationService authorization)
        {
            _roleRepository = roleRepository;
            _authorization = authorization;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            _authorization.EnsurePermission("Roles");
            return await _roleRepository.GetAllRolesAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            _authorization.EnsurePermission("Roles");
            return await _roleRepository.GetRoleByIdAsync(id);
        }

        public async Task<bool> AddRoleAsync(string name, string description)
        {
            _authorization.EnsurePermission("Roles");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty.", nameof(name));

            var existing = await _roleRepository.GetRoleByNameAsync(name);
            if (existing != null) return false;

            var role = new Role { Name = name.Trim(), Description = description };

            // Default permissions (all false) — includes "Roles" and "SalesReport"
            string[] modules = { "Dashboard", "Billing", "Items", "Categories", "Tables", "Ledger", "Journal", "Settings", "Audit", "Shift", "Roles", "SalesReport" };
            foreach (var mod in modules)
            {
                role.Permissions.Add(new RolePermission { ModuleName = mod, CanAccess = false });
            }

            await _roleRepository.AddRoleAsync(role);
            return true;
        }

        public async Task UpdateRolePermissionsAsync(int roleId, List<RolePermission> permissions)
        {
            _authorization.EnsurePermission("Roles");
            await _roleRepository.UpdatePermissionsAsync(roleId, permissions);
        }

        public async Task DeleteRoleAsync(int id)
        {
            _authorization.EnsurePermission("Roles");
            await _roleRepository.DeleteRoleAsync(id);
        }
    }
}
