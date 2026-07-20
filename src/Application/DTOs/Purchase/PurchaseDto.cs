namespace HotelPOS.Application.DTOs.Purchase
{
    public class PurchaseDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PurchaseItemDto
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
