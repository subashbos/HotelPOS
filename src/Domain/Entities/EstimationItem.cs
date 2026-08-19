using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class EstimationItem
    {
        public int Id { get; set; }

        public int EstimationId { get; set; }
        public Estimation? Estimation { get; set; }

        public int ItemId { get; set; }
        public Item? Item { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
    }
}
