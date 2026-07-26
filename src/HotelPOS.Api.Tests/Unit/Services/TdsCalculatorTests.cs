using HotelPOS.Application.Common.Models;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class TdsCalculatorTests
    {
        private static TdsRuleSet Fy2025RuleSet() => new()
        {
            Config = new TdsConfig { FinancialYearStart = 2025, StandardDeduction = 75000m, RebateIncomeLimit = 1200000m, CessRatePercent = 4m },
            Slabs = new List<TdsSlab>
            {
                new TdsSlab { IncomeFrom = 0m, IncomeTo = 400000m, RatePercent = 0m, DisplayOrder = 1 },
                new TdsSlab { IncomeFrom = 400000m, IncomeTo = 800000m, RatePercent = 5m, DisplayOrder = 2 },
                new TdsSlab { IncomeFrom = 800000m, IncomeTo = 1200000m, RatePercent = 10m, DisplayOrder = 3 },
                new TdsSlab { IncomeFrom = 1200000m, IncomeTo = 1600000m, RatePercent = 15m, DisplayOrder = 4 },
                new TdsSlab { IncomeFrom = 1600000m, IncomeTo = 2000000m, RatePercent = 20m, DisplayOrder = 5 },
                new TdsSlab { IncomeFrom = 2000000m, IncomeTo = 2400000m, RatePercent = 25m, DisplayOrder = 6 },
                new TdsSlab { IncomeFrom = 2400000m, IncomeTo = null, RatePercent = 30m, DisplayOrder = 7 }
            }
        };

        [Fact]
        public void CalculateMonthlyTds_NullRuleSet_ReturnsZero()
        {
            var tds = TdsCalculator.CalculateMonthlyTds(200000m, null);

            Assert.Equal(0, tds);
        }

        [Fact]
        public void CalculateMonthlyTds_TaxableIncomeBelowRebateLimit_ReturnsZero()
        {
            // Annual gross 100,000 * 12 = 1,200,000 - 75,000 deduction = 1,125,000, below the 1,200,000 rebate limit.
            var tds = TdsCalculator.CalculateMonthlyTds(100000m, Fy2025RuleSet());

            Assert.Equal(0, tds);
        }

        [Fact]
        public void CalculateMonthlyTds_TaxableIncomeExactlyAtRebateLimit_ReturnsZero()
        {
            // Annual gross 1,275,000 * 12/12... use monthly gross so annual - deduction lands exactly at the limit:
            // (grossMonthly * 12) - 75,000 = 1,200,000  =>  grossMonthly = 106,250.
            var tds = TdsCalculator.CalculateMonthlyTds(106250m, Fy2025RuleSet());

            Assert.Equal(0, tds);
        }

        [Fact]
        public void CalculateMonthlyTds_AboveRebateLimit_AppliesMultiBandSlabsAndCess()
        {
            // Monthly gross 150,000 => annual 1,800,000 - 75,000 deduction = 1,725,000 taxable.
            // Slab tax: 0-4L@0=0, 4-8L@5%=20,000, 8-12L@10%=40,000, 12-16L@15%=60,000,
            // 16-17.25L@20% on 125,000=25,000 => annual tax = 145,000.
            // Cess 4% => 150,800. Monthly = 150,800/12 = 12,566.67.
            var tds = TdsCalculator.CalculateMonthlyTds(150000m, Fy2025RuleSet());

            Assert.Equal(12566.67m, tds);
        }

        [Fact]
        public void CalculateMonthlyTds_TopSlabWithNoUpperBound_IsApplied()
        {
            // Monthly gross 300,000 => annual 3,600,000 - 75,000 = 3,525,000 taxable, well into the
            // uncapped 30% top band (income above 2,400,000).
            var ruleSet = Fy2025RuleSet();
            var tds = TdsCalculator.CalculateMonthlyTds(300000m, ruleSet);

            // Annual tax: 0+20,000+40,000+60,000+80,000+100,000+ (3,525,000-2,400,000)*30% = 300,000+337,500 = 637,500
            // Cess 4% => 663,000. Monthly = 55,250.
            Assert.Equal(55250m, tds);
        }
    }
}
