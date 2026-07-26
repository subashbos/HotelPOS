namespace HotelPOS.Domain.Entities
{
    /// <summary>One income band of a new-regime TDS slab structure for a given financial year.</summary>
    public class TdsSlab
    {
        public int Id { get; set; }

        /// <summary>Calendar year the financial year starts in (April), e.g. 2025 = FY2025-26.</summary>
        public int FinancialYearStart { get; set; }

        public decimal IncomeFrom { get; set; }

        /// <summary>Null means no upper bound (the top slab).</summary>
        public decimal? IncomeTo { get; set; }

        public decimal RatePercent { get; set; }

        public int DisplayOrder { get; set; }
    }
}
