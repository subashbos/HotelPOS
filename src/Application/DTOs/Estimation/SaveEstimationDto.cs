namespace HotelPOS.Application.DTOs.Estimation
{
    /// <summary>DTO used for recording a new customer estimation/quotation via API / ViewModel.</summary>
    public class SaveEstimationDto
    {
        public string EstimationNumber { get; set; } = string.Empty;
        public DateTime EstimationDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        /// <summary>One of <c>EstimationStatuses.All</c>. Defaults to Draft when omitted (create flow);
        /// callers updating an existing estimation must resend its current status to preserve it, or a
        /// new one to transition it (e.g. Draft → Sent → Accepted).</summary>
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<SaveEstimationItemDto> Items { get; set; } = new();
    }

    public class SaveEstimationItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal Discount { get; set; }
    }
}
