#nullable enable

using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly HotelDbContext _context;

        public PayrollRepository(HotelDbContext context)
        {
            _context = context;
        }

        // Left tracked: PayrollService mutates the returned structure in place and persists via
        // UpdateSalaryStructureAsync's blind _context.SalaryStructures.Update(), which throws if
        // this fetch is untracked but the same row is already tracked elsewhere in the DbContext.
        public async Task<SalaryStructure?> GetCurrentSalaryStructureAsync(int employeeId, DateTime asOf)
        {
            return await _context.SalaryStructures
                .Where(s => s.EmployeeId == employeeId
                    && s.EffectiveFrom <= asOf.Date
                    && (s.EffectiveTo == null || s.EffectiveTo >= asOf.Date))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SalaryStructure>> GetSalaryStructuresAsync(int employeeId)
        {
            return await _context.SalaryStructures
                .AsNoTracking()
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.EffectiveFrom)
                .ToListAsync();
        }

        public async Task AddSalaryStructureAsync(SalaryStructure structure)
        {
            _context.SalaryStructures.Add(structure);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSalaryStructureAsync(SalaryStructure structure)
        {
            _context.SalaryStructures.Update(structure);
            await _context.SaveChangesAsync();
        }

        // Left tracked: GetRunAsync's caller (ProcessPayrollAsync) just checks for an existing run
        // before creating one, but GetRunByIdAsync's callers (MarkRunAsPaidAsync/VoidRunAsync) edit
        // the run and its Payslips in memory and persist via UpdateRunAsync's blind
        // _context.PayrollRuns.Update(), which throws if this fetch is untracked but the same row
        // is already tracked elsewhere in the DbContext.
        public async Task<PayrollRun?> GetRunAsync(int month, int year)
        {
            return await _context.PayrollRuns
                .Include(r => r.Payslips)
                .FirstOrDefaultAsync(r => r.Month == month && r.Year == year);
        }

        public async Task<PayrollRun?> GetRunByIdAsync(int id)
        {
            return await _context.PayrollRuns
                .Include(r => r.Payslips)
                    .ThenInclude(p => p.Employee)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<PayrollRun>> GetRunsAsync()
        {
            return await _context.PayrollRuns
                .AsNoTracking()
                .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
                .ToListAsync();
        }

        public async Task AddRunAsync(PayrollRun run)
        {
            _context.PayrollRuns.Add(run);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRunAsync(PayrollRun run)
        {
            _context.PayrollRuns.Update(run);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Payslip>> GetPayslipsByEmployeeAsync(int employeeId)
        {
            return await _context.Payslips
                .AsNoTracking()
                .Include(p => p.PayrollRun)
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PayrollRun!.Year).ThenByDescending(p => p.PayrollRun!.Month)
                .ToListAsync();
        }

        public async Task<TdsRuleSet?> GetTdsRuleSetAsync(int financialYearStart)
        {
            var config = await _context.TdsConfigs
                .AsNoTracking()
                .Where(c => c.FinancialYearStart <= financialYearStart)
                .OrderByDescending(c => c.FinancialYearStart)
                .FirstOrDefaultAsync();
            if (config == null) return null;

            var slabs = await _context.TdsSlabs
                .AsNoTracking()
                .Where(s => s.FinancialYearStart == config.FinancialYearStart)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return new TdsRuleSet { Config = config, Slabs = slabs };
        }
    }
}
