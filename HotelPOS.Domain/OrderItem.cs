using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelPOS.Domain
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        // [JsonIgnore] prevents circular reference if this object is ever serialized
        [JsonIgnore]
        public Order? Order { get; set; }

        public int ItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal TaxPercentage { get; set; }

        public decimal Total { get; set; }
    }
}
