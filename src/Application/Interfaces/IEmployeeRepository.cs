using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<List<Employee>> GetAllAsync();
        Task<Employee?> GetByIdAsync(int id);
        Task<Employee?> GetByCodeAsync(string code);
        Task<Employee?> GetByUserIdAsync(int userId);
        Task AddAsync(Employee employee);
        Task UpdateAsync(Employee employee);
        Task DeleteAsync(int id);
        Task<bool> ExistsByCodeAsync(string code, int excludeId = 0);

        Task<List<Department>> GetDepartmentsAsync();
        Task SaveDepartmentAsync(Department department);
        Task DeleteDepartmentAsync(int id);
        Task<List<Designation>> GetDesignationsAsync();
        Task SaveDesignationAsync(Designation designation);
        Task DeleteDesignationAsync(int id);
    }
}
