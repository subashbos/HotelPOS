using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
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

        public string Status { get; set; } = "Paid";
        public decimal AmountPaid { get; set; }
        public decimal CashPaid { get; set; }
        public decimal CardPaid { get; set; }
        public decimal UpiPaid { get; set; }
        public decimal RefundedAmount { get; set; }
        public string? RefundReason { get; set; }
        public string? VoidReason { get; set; }

        // Customer Details (B2B/GST)
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGstin { get; set; }

        // Auditing & Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        /// <summary>DineIn | Takeaway | Online</summary>
        public string OrderType { get; set; } = "DineIn";
    }
}
