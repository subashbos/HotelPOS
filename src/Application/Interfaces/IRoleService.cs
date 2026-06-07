using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IRoleService
    {
        Task<List<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(int id);
        Task<bool> AddRoleAsync(string name, string description);
        Task UpdateRolePermissionsAsync(int roleId, List<RolePermission> permissions);
        Task DeleteRoleAsync(int id);
    }
}
