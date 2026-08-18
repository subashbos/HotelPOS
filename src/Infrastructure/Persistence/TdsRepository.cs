#nullable enable

using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class TdsRepository : ITdsRepository
    {
        private readonly HotelDbContext _context;

        public TdsRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetFinancialYearsAsync()
        {
            return await _context.TdsConfigs
                .OrderByDescending(c => c.FinancialYearStart)
                .Select(c => c.FinancialYearStart)
                .ToListAsync();
        }

        // TdsService only reads this rule set (SaveRuleSetAsync/DeleteRuleSetAsync do their own
        // separate tracked fetch to mutate), so it's safe to hand back untracked instances.
        public async Task<TdsRuleSet?> GetRuleSetAsync(int financialYearStart)
        {
            var config = await _context.TdsConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FinancialYearStart == financialYearStart);
            if (config == null) return null;

            var slabs = await _context.TdsSlabs
                .AsNoTracking()
                .Where(s => s.FinancialYearStart == financialYearStart)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return new TdsRuleSet { Config = config, Slabs = slabs };
        }

        public async Task SaveRuleSetAsync(TdsRuleSet ruleSet)
        {
            var existingConfig = await _context.TdsConfigs
                .FirstOrDefaultAsync(c => c.FinancialYearStart == ruleSet.Config.FinancialYearStart);

            if (existingConfig == null)
            {
                _context.TdsConfigs.Add(ruleSet.Config);
            }
            else
            {
                existingConfig.StandardDeduction = ruleSet.Config.StandardDeduction;
                existingConfig.RebateIncomeLimit = ruleSet.Config.RebateIncomeLimit;
                existingConfig.CessRatePercent = ruleSet.Config.CessRatePercent;
            }

            var existingSlabs = await _context.TdsSlabs
                .Where(s => s.FinancialYearStart == ruleSet.Config.FinancialYearStart)
                .ToListAsync();
            _context.TdsSlabs.RemoveRange(existingSlabs);
            foreach (var slab in ruleSet.Slabs)
            {
                slab.Id = 0;
                slab.FinancialYearStart = ruleSet.Config.FinancialYearStart;
            }
            _context.TdsSlabs.AddRange(ruleSet.Slabs);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteRuleSetAsync(int financialYearStart)
        {
            var config = await _context.TdsConfigs
                .FirstOrDefaultAsync(c => c.FinancialYearStart == financialYearStart);
            if (config != null) _context.TdsConfigs.Remove(config);

            var slabs = await _context.TdsSlabs
                .Where(s => s.FinancialYearStart == financialYearStart)
                .ToListAsync();
            _context.TdsSlabs.RemoveRange(slabs);

            await _context.SaveChangesAsync();
        }
    }
}
