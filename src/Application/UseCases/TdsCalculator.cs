using HotelPOS.Application.Common.Models;

namespace HotelPOS.Application.UseCases
{
    /// <summary>
    /// Computes monthly income-tax TDS under the new regime (no 80C/HRA declarations) from a
    /// financial year's slab structure. Pure calculation, no I/O — safe to unit test directly.
    /// </summary>
    public static class TdsCalculator
    {
        public static decimal CalculateMonthlyTds(decimal grossMonthly, TdsRuleSet? ruleSet)
        {
            if (ruleSet == null) return 0;

            var annualGross = grossMonthly * 12;
            var taxableIncome = Math.Max(0, annualGross - ruleSet.Config.StandardDeduction);

            // Section 87A: taxable income at or below the rebate limit owes no tax at all under
            // the new regime — this is a documented simplification of the marginal-relief rule
            // right at the threshold, matching the approximation style used elsewhere for PT.
            if (taxableIncome <= ruleSet.Config.RebateIncomeLimit) return 0;

            decimal annualTax = 0;
            foreach (var slab in ruleSet.Slabs.OrderBy(s => s.DisplayOrder))
            {
                if (taxableIncome <= slab.IncomeFrom) break;

                var bandTop = slab.IncomeTo ?? taxableIncome;
                var bandTaxable = Math.Min(taxableIncome, bandTop) - slab.IncomeFrom;
                if (bandTaxable <= 0) continue;

                annualTax += bandTaxable * slab.RatePercent / 100m;
            }

            var annualTaxWithCess = annualTax * (1 + ruleSet.Config.CessRatePercent / 100m);
            return Math.Round(annualTaxWithCess / 12, 2);
        }
    }
}
