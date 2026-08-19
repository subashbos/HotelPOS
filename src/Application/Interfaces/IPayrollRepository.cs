using HotelPOS.Application.Common.Models;
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

        /// <summary>
        /// Resolves the TDS config + slabs for a financial year, falling back to the closest
        /// earlier configured year if the exact year hasn't been set up yet, or null if none exist.
        /// </summary>
        Task<TdsRuleSet?> GetTdsRuleSetAsync(int financialYearStart);
    }
}
