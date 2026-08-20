using HotelPOS.Domain.Common.Constants;

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
        public string PaymentMode { get; set; } = PaymentModes.Cash;
        public string OrderType { get; set; } = OrderTypes.DineIn;
        public string Status { get; set; } = OrderStatuses.Paid;
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
        public string PaymentType { get; set; } = PaymentModes.Cash;
    }

    /// <summary>One row = one tax rate present on one B2B invoice, matching the official GSTR-1
    /// B2B (4A/4B/4C/6B/6C) invoice-wise filing format.</summary>
    public class GstR1RowDto
    {
        public int SNo { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal InvoiceValue { get; set; }
        public string Pos { get; set; } = string.Empty;
        public string ReverseCharge { get; set; } = "N";
        public string InvoiceType { get; set; } = "R";
        public string CustomerName { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
    }

    /// <summary>One row per tax rate, aggregating all B2C (no customer GSTIN) invoices in the
    /// period - matches the GSTR-1 table 7 (B2C Small) summary format.</summary>
    public class GstR1B2cSummaryDto
    {
        public decimal Rate { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal TotalTax => Cgst + Sgst + Igst;
        public decimal TotalValue => TaxableValue + TotalTax;
    }

    /// <summary>One row per HSN code and tax rate, aggregating ALL outward supplies (B2B and B2C
    /// combined) in the period - matches the GSTR-1 table 12 (HSN-wise summary) format.</summary>
    public class HsnSummaryRowDto
    {
        public string HsnCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Uqc { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal Rate { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal TotalTax => Cgst + Sgst + Igst;
        public decimal TotalValue => TaxableValue + TotalTax;
    }

    /// <summary>Composite payload for the GSTR-1 report page: invoice-wise B2B rows, the B2C(Small)
    /// rate-wise summary, and the HSN-wise summary — the three tables the API exposes together so
    /// the client can render all three tabs from a single request.</summary>
    public class GstR1ReportDto
    {
        public List<GstR1RowDto> B2BRows { get; set; } = new();
        public List<GstR1B2cSummaryDto> B2cSummary { get; set; } = new();
        public List<HsnSummaryRowDto> HsnSummary { get; set; } = new();
    }
}
