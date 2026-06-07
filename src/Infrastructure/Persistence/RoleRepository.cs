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
            _context.RolePermissions.RemoveRange(existing);
            
            // Ensure we only save distinct modules to prevent duplicates in the DB
            // Prioritize 'true' permissions if duplicates exist
            var distinctPermissions = permissions
                .OrderByDescending(p => p.CanAccess)
                .GroupBy(p => p.ModuleName)
                .Select(g => g.First())
                .ToList();

            foreach(var p in distinctPermissions)
            {
                p.RoleId = roleId;
                p.Id = 0; // Ensure new identity
            }
            
            _context.RolePermissions.AddRange(distinctPermissions);
            await _context.SaveChangesAsync();
        }
    }
}
