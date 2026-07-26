namespace HotelPOS.Application.DTOs.Tds
{
    public class TdsSlabDto
    {
        public decimal IncomeFrom { get; set; }
        public decimal? IncomeTo { get; set; }
        public decimal RatePercent { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class TdsRuleSetDto
    {
        public int FinancialYearStart { get; set; }
        public decimal StandardDeduction { get; set; }
        public decimal RebateIncomeLimit { get; set; }
        public decimal CessRatePercent { get; set; }
        public List<TdsSlabDto> Slabs { get; set; } = new();
    }

    /// <summary>Request body to create/replace a financial year's TDS rule set.</summary>
    public class SaveTdsRuleSetDto
    {
        public decimal StandardDeduction { get; set; }
        public decimal RebateIncomeLimit { get; set; }
        public decimal CessRatePercent { get; set; }
        public List<TdsSlabDto> Slabs { get; set; } = new();
    }
}
