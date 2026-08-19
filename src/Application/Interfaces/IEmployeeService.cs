using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task SaveEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(int id);
        Task<bool> ValidateEmployeeCodeUniqueAsync(string code, int excludeId = 0);
        Task<List<Department>> GetDepartmentsAsync();
        Task SaveDepartmentAsync(Department department);
        Task DeleteDepartmentAsync(int id);
        Task<List<Designation>> GetDesignationsAsync();
        Task SaveDesignationAsync(Designation designation);
        Task DeleteDesignationAsync(int id);
    }
}
