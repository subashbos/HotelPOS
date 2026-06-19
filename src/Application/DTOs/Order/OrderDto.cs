using System;
using System.Collections.Generic;

namespace HotelPOS.Application.DTOs.Order
{
    public class OrderDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? FiscalYear { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TableNumber { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
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
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGstin { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string OrderType { get; set; } = "DineIn";
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal Total { get; set; }
    }
}
