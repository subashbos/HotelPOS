using HotelPOS.Application.Common.Models;

namespace HotelPOS.Application.Interfaces
{
    public interface ITdsRepository
    {
        Task<List<int>> GetFinancialYearsAsync();

        /// <summary>Exact match only — no fallback to an earlier year (unlike <see cref="IPayrollRepository.GetTdsRuleSetAsync"/>, which is used by payroll runs).</summary>
        Task<TdsRuleSet?> GetRuleSetAsync(int financialYearStart);

        /// <summary>Upserts the config and replaces all slab rows for the given financial year.</summary>
        Task SaveRuleSetAsync(TdsRuleSet ruleSet);

        Task DeleteRuleSetAsync(int financialYearStart);
    }
}
