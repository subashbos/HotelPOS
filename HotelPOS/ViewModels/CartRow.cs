namespace HotelPOS.ViewModels
{
    /// <summary>Cart display row with serial number and ItemId for cart operations.</summary>
    public class CartRow
    {
        public int SNo { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
    }
}
