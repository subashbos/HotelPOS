using HotelPOS.Application.Common.Models;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    /// <summary>Admin CRUD over TDS slab structures. Authorization is enforced by the Admin-only controller route.</summary>
    public class TdsService : ITdsService
    {
        private readonly ITdsRepository _repository;

        public TdsService(ITdsRepository repository)
        {
            _repository = repository;
        }

        public Task<List<int>> GetFinancialYearsAsync() => _repository.GetFinancialYearsAsync();

        public Task<TdsRuleSet?> GetRuleSetAsync(int financialYearStart) => _repository.GetRuleSetAsync(financialYearStart);

        public async Task SaveRuleSetAsync(TdsRuleSet ruleSet)
        {
            if (ruleSet == null) throw new ArgumentNullException(nameof(ruleSet));
            ValidateConfig(ruleSet);

            var ordered = ruleSet.Slabs.OrderBy(s => s.DisplayOrder).ToList();
            ValidateSlabs(ordered);

            await _repository.SaveRuleSetAsync(ruleSet);
        }

        public Task DeleteRuleSetAsync(int financialYearStart) => _repository.DeleteRuleSetAsync(financialYearStart);

        private static void ValidateConfig(TdsRuleSet ruleSet)
        {
            if (ruleSet.Config.StandardDeduction < 0) throw new ArgumentException("Standard deduction cannot be negative.");
            if (ruleSet.Config.RebateIncomeLimit < 0) throw new ArgumentException("Rebate income limit cannot be negative.");
            if (ruleSet.Config.CessRatePercent < 0) throw new ArgumentException("Cess rate cannot be negative.");
            if (ruleSet.Slabs == null || ruleSet.Slabs.Count == 0) throw new ArgumentException("At least one slab band is required.");
        }

        private static void ValidateSlabs(List<TdsSlab> ordered)
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                var slab = ordered[i];
                if (slab.RatePercent < 0 || slab.RatePercent > 100)
                    throw new ArgumentException("Slab rate must be between 0 and 100.");
                if (slab.IncomeFrom < 0) throw new ArgumentException("Slab income-from cannot be negative.");
                if (slab.IncomeTo.HasValue && slab.IncomeTo.Value <= slab.IncomeFrom)
                    throw new ArgumentException("Slab income-to must be greater than income-from.");
                if (i > 0 && slab.IncomeFrom != ordered[i - 1].IncomeTo)
                    throw new ArgumentException("Slab bands must be contiguous, ordered by DisplayOrder.");
                if (i == ordered.Count - 1 && slab.IncomeTo.HasValue)
                    throw new ArgumentException("The highest slab band must have no upper bound.");
            }
        }
    }
}
