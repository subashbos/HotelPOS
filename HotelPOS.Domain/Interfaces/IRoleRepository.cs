using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<List<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(int id);
        Task<Role?> GetRoleByNameAsync(string name);
        Task AddRoleAsync(Role role);
        Task UpdateRoleAsync(Role role);
        Task DeleteRoleAsync(int id);
        Task<List<RolePermission>> GetPermissionsByRoleIdAsync(int roleId);
        Task UpdatePermissionsAsync(int roleId, List<RolePermission> permissions);
    }
}
