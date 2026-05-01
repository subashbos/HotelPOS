namespace HotelPOS.Application
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
        public int TableNumber { get; set; }
        public DateTime CreatedAt { get; set; }   // stored as local time for display
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
        public List<HotelPOS.Domain.OrderItem> Items { get; set; } = new();
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
}
