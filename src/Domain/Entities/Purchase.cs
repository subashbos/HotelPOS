using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class Purchase
    {
        public int Id { get; set; }
        
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;
        
        public DateTime PurchaseDate { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PaymentType { get; set; } = "Cash"; // Cash, Credit, UPI
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public decimal Subtotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal GrandTotal { get; set; }
        
        public List<PurchaseItem> PurchaseItems { get; set; } = new();
    }
}
