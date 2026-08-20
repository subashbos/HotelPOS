using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IAuthorizationService _authorization;
        private readonly IValidator<Employee> _validator;

        public EmployeeService(IEmployeeRepository repository, IAuthorizationService authorization, IValidator<Employee>? validator = null)
        {
            _repository = repository;
            _authorization = authorization;
            _validator = validator ?? new EmployeeValidator();
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            // Full employee records (PAN, Aadhaar, bank details) — gated behind HrEmployees so a
            // lower-trust Employee Self-Service login can't list every coworker's PII. Viewing
            // one's own profile goes through EssController instead, which never calls this.
            _authorization.EnsurePermission(PermissionModules.HrEmployees);
            return await _repository.GetAllAsync() ?? new List<Employee>();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            _authorization.EnsurePermission(PermissionModules.HrEmployees);
            return await _repository.GetByIdAsync(id);
        }

        public async Task SaveEmployeeAsync(Employee employee)
        {
            _authorization.EnsureEditPermission(PermissionModules.HrEmployees);

            if (employee == null) throw new ArgumentNullException(nameof(employee));

            employee.EmployeeCode = employee.EmployeeCode?.Trim() ?? string.Empty;
            employee.FirstName = employee.FirstName?.Trim() ?? string.Empty;
            employee.LastName = employee.LastName?.Trim();

            if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
                employee.EmployeeCode = await GenerateNextEmployeeCodeAsync();

            var result = _validator.Validate(employee);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            if (await _repository.ExistsByCodeAsync(employee.EmployeeCode, employee.Id))
                throw new ArgumentException($"An employee with code '{employee.EmployeeCode}' already exists.");

            if (employee.Id == 0)
                await _repository.AddAsync(employee);
            else
                await _repository.UpdateAsync(employee);
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            _authorization.EnsureDeletePermission(PermissionModules.HrEmployees);

            _ = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Employee #{id} not found.");
            await _repository.DeleteAsync(id);
        }

        public async Task<bool> ValidateEmployeeCodeUniqueAsync(string code, int excludeId = 0)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            return !await _repository.ExistsByCodeAsync(code.Trim(), excludeId);
        }

        public async Task<List<Department>> GetDepartmentsAsync()
        {
            return await _repository.GetDepartmentsAsync();
        }

        public async Task SaveDepartmentAsync(Department department)
        {
            if (department == null) throw new ArgumentNullException(nameof(department));
            if (string.IsNullOrWhiteSpace(department.Name)) throw new ArgumentException("Department name is required.");
            await _repository.SaveDepartmentAsync(department);
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            await _repository.DeleteDepartmentAsync(id);
        }

        public async Task<List<Designation>> GetDesignationsAsync()
        {
            return await _repository.GetDesignationsAsync();
        }

        public async Task SaveDesignationAsync(Designation designation)
        {
            if (designation == null) throw new ArgumentNullException(nameof(designation));
            if (string.IsNullOrWhiteSpace(designation.Title)) throw new ArgumentException("Designation title is required.");
            if (designation.DepartmentId <= 0) throw new ArgumentException("Please select a valid department.");
            await _repository.SaveDesignationAsync(designation);
        }

        public async Task DeleteDesignationAsync(int id)
        {
            await _repository.DeleteDesignationAsync(id);
        }

        private async Task<string> GenerateNextEmployeeCodeAsync()
        {
            var all = await _repository.GetAllAsync() ?? new List<Employee>();
            var maxSeq = 0;
            foreach (var code in all.Select(e => e.EmployeeCode))
            {
                if (code.StartsWith("EMP", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(code.AsSpan(3), out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }
            return $"EMP{(maxSeq + 1):D4}";
        }
    }
}
