using System;
using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class WastageEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }
        public Item? Item { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = "Spoilage"; // Spoilage, Overproduction, ManualAdjustment, Damage

        [Required]
        public DateTime WastedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public decimal CostPerUnit { get; set; }

        public string? Notes { get; set; }
    }
}
