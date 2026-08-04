namespace HotelPOS.Application.DTOs.Estimation
{
    public class EstimationDto
    {
        public int Id { get; set; }
        public string EstimationNumber { get; set; } = string.Empty;
        public DateTime EstimationDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        public int? ConvertedOrderId { get; set; }
        public List<EstimationItemDto> Items { get; set; } = new();
    }

    public class EstimationItemDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }
}
