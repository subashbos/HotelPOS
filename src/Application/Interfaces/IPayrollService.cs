using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IPayrollService
    {
        Task<List<SalaryStructure>> GetSalaryStructuresAsync(int employeeId);
        Task SaveSalaryStructureAsync(SalaryStructure structure);

        Task<PayrollRun> RunPayrollAsync(int month, int year, int? processedByUserId);
        Task MarkRunAsPaidAsync(int runId);
        Task<List<PayrollRun>> GetRunsAsync();
        Task<PayrollRun?> GetRunByIdAsync(int id);
        Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId);

        /// <summary>
        /// Pure calculation of a single employee's payslip for the given attendance window —
        /// no I/O, safe to unit test directly against Indian PF/ESI/PT rules.
        /// </summary>
        Payslip CalculatePayslip(SalaryStructure structure, decimal workingDays, decimal paidDays);
    }
}
