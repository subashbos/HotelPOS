namespace HotelPOS.Application.DTOs.Customer
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gstin { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>DTO used for creating or updating a Customer via API / ViewModel.</summary>
    public class SaveCustomerDto
    {
        public int Id { get; set; }          // 0 = new, >0 = update
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gstin { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>A customer's aggregated order history, used to drive repeat-visit and spend insights.</summary>
    public class CustomerHistoryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? FirstOrderDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public List<CustomerOrderSummaryDto> Orders { get; set; } = new();
    }

    public class CustomerOrderSummaryDto
    {
        public int OrderId { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
    }
}
