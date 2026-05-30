using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _roleRepository.GetAllRolesAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _roleRepository.GetRoleByIdAsync(id);
        }

        public async Task<bool> AddRoleAsync(string name, string description)
        {
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
            await _roleRepository.UpdatePermissionsAsync(roleId, permissions);
        }

        public async Task DeleteRoleAsync(int id)
        {
            await _roleRepository.DeleteRoleAsync(id);
        }
    }
}
