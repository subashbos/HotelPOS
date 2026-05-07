using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class Order
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }

        public string? InvoiceNumber { get; set; }

        public string? FiscalYear { get; set; }

        public DateTime CreatedAt { get; set; }

        public int TableNumber { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public decimal Subtotal { get; set; }

        public decimal GstAmount { get; set; }
        public decimal CgstAmount { get; set; }
        public decimal SgstAmount { get; set; }
        public decimal IgstAmount { get; set; }

        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMode { get; set; } = "Cash";

        // Customer Details (B2B/GST)
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGstin { get; set; }

        // Auditing & Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
