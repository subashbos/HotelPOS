namespace HotelPOS.Application.DTOs.Purchase
{
    /// <summary>DTO used for recording a new purchase invoice via API / ViewModel.</summary>
    public class SavePurchaseDto
    {
        public int SupplierId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal TotalDiscount { get; set; }
        public List<SavePurchaseItemDto> Items { get; set; } = new();
    }

    public class SavePurchaseItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal Discount { get; set; }
    }
}
