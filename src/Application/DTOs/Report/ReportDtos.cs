namespace HotelPOS.Application.DTOs.Report
{
    /// <summary>Top-level aggregated numbers shown in the dashboard header cards.</summary>
    public class SalesReportDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string MostPopularItem { get; set; } = "N/A";

        public List<TableSalesRowDto> SalesByTable { get; set; } = new();
        public List<RecentOrderRowDto> RecentOrders { get; set; } = new();
        public List<CategorySalesRowDto> SalesByCategory { get; set; } = new();
        public List<PaymentModeSalesRowDto> SalesByPaymentMode { get; set; } = new();
    }

    public class PaymentModeSalesRowDto
    {
        public int SNo { get; set; }
        public string PaymentMode { get; set; } = "Unknown";
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public double Percentage { get; set; }
    }

    public class CategorySalesRowDto
    {
        public int SNo { get; set; }
        public string CategoryName { get; set; } = "Unknown";
        public decimal Revenue { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>One row in the "Sales by Table" grid.</summary>
    public class TableSalesRowDto
    {
        public int SNo { get; set; }
        public int TableNumber { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>One row in the "Recent Orders" grid.</summary>
    public class RecentOrderRowDto
    {
        public int SNo { get; set; }
        public int OrderId { get; set; }
        public string? InvoiceNumber { get; set; }
        public int TableNumber { get; set; }
        public DateTime CreatedAt { get; set; }   // stored as local time for display
        public decimal Total { get; set; }
        public decimal DiscountAmount { get; set; }
        public int ItemCount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string OrderType { get; set; } = "DineIn";
        public string Status { get; set; } = "Paid";
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGstin { get; set; }
        public List<HotelPOS.Domain.Entities.OrderItem> Items { get; set; } = new();
    }

    /// <summary>One row in the "Item Report" grid.</summary>
    public class ItemReportRowDto
    {
        public int SNo { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int TotalQtySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal UnitPrice { get; set; }
    }

    /// <summary>One row in the GST report.</summary>
    public class GstReportRowDto
    {
        public int SNo { get; set; }
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal GstAmount { get; set; }
        public decimal NetIncome { get; set; }
    }

    /// <summary>Simple data point for monthly sales trends.</summary>
    public class MonthlySalesChartDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class PurchaseReportRowDto
    {
        public int SNo { get; set; }
        public int PurchaseId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentType { get; set; } = "Cash";
    }
}
