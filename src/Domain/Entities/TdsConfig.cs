namespace HotelPOS.Domain.Entities
{
    /// <summary>New-regime TDS parameters for a financial year — pairs with the <see cref="TdsSlab"/> bands for the same year.</summary>
    public class TdsConfig
    {
        public int Id { get; set; }

        /// <summary>Calendar year the financial year starts in (April), e.g. 2025 = FY2025-26.</summary>
        public int FinancialYearStart { get; set; }

        public decimal StandardDeduction { get; set; }

        /// <summary>Section 87A: total taxable income at or below this is fully rebated (tax = 0).</summary>
        public decimal RebateIncomeLimit { get; set; }

        public decimal CessRatePercent { get; set; }
    }
}
