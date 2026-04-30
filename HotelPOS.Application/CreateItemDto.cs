using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Application
{
    public class CreateItemDto
    {
        [Required(ErrorMessage = "Item name is required.")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Item name must be between 1 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        public decimal TaxPercentage { get; set; }

        public string? HsnCode { get; set; }

        public int? CategoryId { get; set; }
        public int StockQuantity { get; set; }
        public bool TrackInventory { get; set; }
        public string? Barcode { get; set; }
    }
}
