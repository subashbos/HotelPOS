using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    public class Item
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public decimal TaxPercentage { get; set; }

        [MaxLength(20)]
        public string? HsnCode { get; set; }


        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public int StockQuantity { get; set; }
        public bool TrackInventory { get; set; }

        public decimal CostPrice { get; set; }
        public int MinStockThreshold { get; set; } = 10;

        [MaxLength(100)]
        public string? Barcode { get; set; }
    }
}
