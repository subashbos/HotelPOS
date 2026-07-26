using HotelPOS.Application.Common.Models;

namespace HotelPOS.Application.Interfaces
{
    public interface ITdsService
    {
        Task<List<int>> GetFinancialYearsAsync();
        Task<TdsRuleSet?> GetRuleSetAsync(int financialYearStart);
        Task SaveRuleSetAsync(TdsRuleSet ruleSet);
        Task DeleteRuleSetAsync(int financialYearStart);
    }
}
