#nullable enable

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
                .Include(p => p.PayrollRun)
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.PayrollRun!.Year).ThenByDescending(p => p.PayrollRun!.Month)
                .ToListAsync();
        }
    }
}
