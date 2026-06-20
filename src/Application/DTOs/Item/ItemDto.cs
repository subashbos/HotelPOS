namespace HotelPOS.Application.DTOs.Item
{
    public class ItemDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal TaxPercentage { get; set; }
        public string? HsnCode { get; set; }
        public int? CategoryId { get; set; }
        public int StockQuantity { get; set; }
        public bool TrackInventory { get; set; }
        public decimal CostPrice { get; set; }
        public int MinStockThreshold { get; set; } = 10;
        public string? Barcode { get; set; }
    }
}
