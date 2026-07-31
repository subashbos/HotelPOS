using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class RoleRepository : IRoleRepository
    {
        private readonly HotelDbContext _context;

        public RoleRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.Include(r => r.Permissions).ToListAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _context.Roles.Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Role?> GetRoleByNameAsync(string name)
        {
            return await _context.Roles.Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower());
        }

        public async Task AddRoleAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoleAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<RolePermission>> GetPermissionsByRoleIdAsync(int roleId)
        {
            return await _context.RolePermissions
                .Where(p => p.RoleId == roleId)
                .ToListAsync();
        }

        public async Task UpdatePermissionsAsync(int roleId, List<RolePermission> permissions)
        {
            var existing = await _context.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync();
            var existingByModule = existing.ToDictionary(p => p.ModuleName);

            // Ensure we only save distinct modules to prevent duplicates in the DB
            // Prioritize 'true' permissions if duplicates exist
            var distinctPermissions = permissions
                .OrderByDescending(p => p.CanAccess)
                .GroupBy(p => p.ModuleName)
                .Select(g => g.First())
                .ToList();

            // Update matching modules in place and add only genuinely new ones, instead of
            // delete-all/reinsert-with-new-identity: that pattern let a partially-applied
            // delete leave stale rows behind, and the fresh identity values it consumed could
            // collide with the fixed Ids that later EF migrations hardcode for new permission
            // modules (see PK_RolePermissions violation on Id 45 after AddTdsPermission).
            foreach (var p in distinctPermissions)
            {
                if (existingByModule.TryGetValue(p.ModuleName, out var row))
                {
                    row.CanAccess = p.CanAccess;
                    row.CanEdit = p.CanEdit;
                    row.CanDelete = p.CanDelete;
                }
                else
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        ModuleName = p.ModuleName,
                        CanAccess = p.CanAccess,
                        CanEdit = p.CanEdit,
                        CanDelete = p.CanDelete
                    });
                }
            }

            var incomingModules = new HashSet<string>(distinctPermissions.Select(p => p.ModuleName));
            var toRemove = existing.Where(e => !incomingModules.Contains(e.ModuleName)).ToList();
            _context.RolePermissions.RemoveRange(toRemove);

            await _context.SaveChangesAsync();
        }
    }
}
