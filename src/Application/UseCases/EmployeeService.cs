using FluentValidation;
using HotelPOS.Application.Common.Validators;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IValidator<Employee> _validator;

        public EmployeeService(IEmployeeRepository repository, IValidator<Employee>? validator = null)
        {
            _repository = repository;
            _validator = validator ?? new EmployeeValidator();
        }

        public async Task<List<Employee>> GetEmployeesAsync()
        {
            return await _repository.GetAllAsync() ?? new List<Employee>();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task SaveEmployeeAsync(Employee employee)
        {
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
            var existing = await _repository.GetByIdAsync(id)
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

        public async Task<List<Designation>> GetDesignationsAsync()
        {
            return await _repository.GetDesignationsAsync();
        }

        private async Task<string> GenerateNextEmployeeCodeAsync()
        {
            var all = await _repository.GetAllAsync() ?? new List<Employee>();
            var maxSeq = 0;
            foreach (var e in all)
            {
                if (e.EmployeeCode.StartsWith("EMP", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(e.EmployeeCode.AsSpan(3), out var seq) && seq > maxSeq)
                {
                    maxSeq = seq;
                }
            }
            return $"EMP{(maxSeq + 1):D4}";
        }
    }
}
