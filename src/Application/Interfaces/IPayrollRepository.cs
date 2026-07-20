using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IPayrollRepository
    {
        Task<SalaryStructure?> GetCurrentSalaryStructureAsync(int employeeId, DateTime asOf);
        Task<List<SalaryStructure>> GetSalaryStructuresAsync(int employeeId);
        Task AddSalaryStructureAsync(SalaryStructure structure);
        Task UpdateSalaryStructureAsync(SalaryStructure structure);

        Task<PayrollRun?> GetRunAsync(int month, int year);
        Task<PayrollRun?> GetRunByIdAsync(int id);
        Task<List<PayrollRun>> GetRunsAsync();
        Task AddRunAsync(PayrollRun run);
        Task UpdateRunAsync(PayrollRun run);

        Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId);
    }
}
