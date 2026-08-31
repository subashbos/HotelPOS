using HotelPOS.Api.Export;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Sales, purchase, GST, margin and wastage reports — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class ReportsController : BaseApiController
    {
        private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IReportService _reportService;
        private readonly IBIReportService _biReportService;

        public ReportsController(IReportService reportService, IBIReportService biReportService)
        {
            _reportService = reportService;
            _biReportService = biReportService;
        }

        [HttpGet("sales")]
        public async Task<ActionResult<SalesReportDto>> GetSalesReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _reportService.GetSalesReportAsync(from, to));
        }

        [HttpGet("sales/export")]
        public async Task<IActionResult> ExportSalesReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var report = await _reportService.GetSalesReportAsync(from, to);

            var ordersSheet = new ExcelSheet(
                "Recent Orders",
                new[] { "Date", "Invoice", "Customer", "Table", "Type", "Status", "Payment", "Total Amount" },
                report.RecentOrders.Select(r => (IReadOnlyList<object?>)new object?[]
                {
                    r.CreatedAt.ToString("dd MMM yyyy HH:mm"), r.InvoiceNumber ?? string.Empty, r.CustomerName ?? "N/A",
                    r.TableNumber, r.OrderType, r.Status, r.PaymentMode, r.Total
                }).ToList());

            var categorySheet = new ExcelSheet(
                "Sales by Category",
                new[] { "Category", "Revenue", "%" },
                report.SalesByCategory.Select(c => (IReadOnlyList<object?>)new object?[]
                {
                    c.CategoryName, c.Revenue, c.Percentage
                }).ToList());

            var paymentModeSheet = new ExcelSheet(
                "Sales by Payment Mode",
                new[] { "Mode", "Orders", "Revenue" },
                report.SalesByPaymentMode.Select(p => (IReadOnlyList<object?>)new object?[]
                {
                    p.PaymentMode, p.OrderCount, p.Revenue
                }).ToList());

            var bytes = ExcelExportBuilder.Build(ordersSheet, categorySheet, paymentModeSheet);

            return File(bytes, ExcelContentType, $"Sales_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("items")]
        public async Task<ActionResult<List<ItemReportRowDto>>> GetItemReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _reportService.GetItemReportAsync(from, to));
        }

        [HttpGet("items/export")]
        public async Task<IActionResult> ExportItemReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var rows = await _reportService.GetItemReportAsync(from, to);
            var sheetRows = rows.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.ItemName, r.TotalQtySold, r.UnitPrice, r.TotalRevenue
            }).ToList();

            var bytes = ExcelExportBuilder.Build(new ExcelSheet(
                "Item Report",
                new[] { "Item", "Qty Sold", "Unit Price", "Revenue" },
                sheetRows));

            return File(bytes, ExcelContentType, $"Item_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("gst")]
        public async Task<ActionResult<List<GstReportRowDto>>> GetGstReport([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            return Ok(await _reportService.GetGstReportAsync(from, to));
        }

        [HttpGet("gst/export")]
        public async Task<IActionResult> ExportGstReport([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var rows = await _reportService.GetGstReportAsync(from, to);
            var sheetRows = rows.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.Date.ToString("dd MMM yyyy"), r.OrderCount, r.GrossRevenue, r.GstAmount, r.NetIncome
            }).ToList();

            var bytes = ExcelExportBuilder.Build(new ExcelSheet(
                "Ledger",
                new[] { "Date", "Orders", "Gross Revenue", "GST", "Net Income" },
                sheetRows));

            return File(bytes, ExcelContentType, $"Ledger_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("gstr1")]
        public async Task<ActionResult<GstR1ReportDto>> GetGstR1Report([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            return Ok(await _reportService.GetGstR1ReportAsync(from, to));
        }

        [HttpGet("gstr1/export")]
        public async Task<IActionResult> ExportGstR1Report([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var report = await _reportService.GetGstR1ReportAsync(from, to);

            var b2bSheet = new ExcelSheet(
                "B2B Invoices",
                new[] { "S.No", "GSTIN", "Invoice Number", "Date", "Invoice Value", "POS", "Reverse Charge", "Invoice Type", "Customer", "Taxable Value", "Item Total", "Rate", "CGST", "SGST", "IGST" },
                report.B2BRows.Select(r => (IReadOnlyList<object?>)new object?[]
                {
                    r.SNo, r.Gstin, r.InvoiceNumber, r.Date.ToString("dd-MM-yyyy"), r.InvoiceValue, r.Pos, r.ReverseCharge,
                    r.InvoiceType, r.CustomerName, r.TaxableValue, r.ItemTotal, r.Rate, r.Cgst, r.Sgst, r.Igst
                }).ToList());

            var b2cSheet = new ExcelSheet(
                "B2C Summary",
                new[] { "Rate", "No. of Invoices", "Taxable Value", "CGST", "SGST", "IGST", "Total Tax", "Total Value" },
                report.B2cSummary.Select(s => (IReadOnlyList<object?>)new object?[]
                {
                    s.Rate, s.InvoiceCount, s.TaxableValue, s.Cgst, s.Sgst, s.Igst, s.TotalTax, s.TotalValue
                }).ToList());

            var hsnSheet = new ExcelSheet(
                "HSN Summary",
                new[] { "HSN Code", "Description", "UQC", "Total Quantity", "Taxable Value", "Rate", "CGST", "SGST", "IGST", "Total Tax", "Total Value" },
                report.HsnSummary.Select(h => (IReadOnlyList<object?>)new object?[]
                {
                    h.HsnCode, h.Description, h.Uqc, h.TotalQuantity, h.TaxableValue, h.Rate, h.Cgst, h.Sgst, h.Igst, h.TotalTax, h.TotalValue
                }).ToList());

            var bytes = ExcelExportBuilder.Build(b2bSheet, b2cSheet, hsnSheet);
            return File(bytes, ExcelContentType, $"GSTR1_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("monthly-chart")]
        public async Task<ActionResult<List<MonthlySalesChartDto>>> GetMonthlyChart()
        {
            return Ok(await _reportService.GetMonthlyChartDataAsync());
        }

        [HttpGet("purchases")]
        public async Task<ActionResult<PagedPurchaseReportResponse>> GetPurchaseReport([FromQuery] PurchaseReportQueryRequest request)
        {
            var query = new PagedPurchaseReportRequest(
                request.Page ?? 1,
                request.PageSize ?? 20,
                request.From,
                request.To,
                request.SupplierId,
                request.ItemName,
                request.PaymentType,
                request.InvoiceNo);

            var (items, totalCount, totalPurchases, totalTax, totalDiscount, totalQty) =
                await _reportService.GetPagedPurchaseReportAsync(query);

            return Ok(new PagedPurchaseReportResponse
            {
                Items = items,
                TotalCount = totalCount,
                TotalPurchases = totalPurchases,
                TotalTax = totalTax,
                TotalDiscount = totalDiscount,
                TotalQty = totalQty
            });
        }

        [HttpGet("purchases/export")]
        public async Task<IActionResult> ExportPurchaseReport([FromQuery] PurchaseReportQueryRequest request)
        {
            // pageSize -1: export every matching row, not just the current page of the on-screen grid.
            var query = new PagedPurchaseReportRequest(
                1, -1, request.From, request.To, request.SupplierId, request.ItemName, request.PaymentType, request.InvoiceNo);

            var (items, _, _, _, _, _) = await _reportService.GetPagedPurchaseReportAsync(query);

            var rows = items.Select(r => (IReadOnlyList<object?>)new object?[]
            {
                r.PurchaseDate.ToString("g"), r.InvoiceNumber, r.SupplierName, r.ItemName, r.Quantity,
                r.UnitPrice, r.TaxAmount, r.Discount, r.TotalAmount, r.PaymentType
            }).ToList();

            var bytes = ExcelExportBuilder.Build(new ExcelSheet(
                "Purchase Report",
                new[] { "Date", "Invoice No", "Supplier", "Item Name", "Qty", "Price", "Tax Amount", "Discount", "Total Amount", "Payment" },
                rows));

            return File(bytes, ExcelContentType, $"Purchase_Report_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("margins/summary")]
        public async Task<ActionResult<ProfitMarginSummaryDto>> GetMarginSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetProfitMarginSummaryAsync(from, to));
        }

        [HttpGet("margins/items")]
        public async Task<ActionResult<List<ItemMarginRowDto>>> GetItemMargins([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetItemMarginsAsync(from, to));
        }

        [HttpGet("wastage")]
        public async Task<ActionResult<WastageSummaryDto>> GetWastageSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetWastageSummaryAsync(from, to));
        }

        [HttpPost("wastage")]
        public async Task<IActionResult> LogWastage([FromBody] LogWastageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("A wastage reason is required.");
            if ((request.Quantity ?? 0) <= 0) return BadRequest("Quantity must be greater than zero.");

            await _biReportService.LogWastageAsync(request.ItemId ?? 0, request.Quantity ?? 0, request.Reason, request.Notes);
            return NoContent();
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<List<LowStockAlertDto>>> GetLowStockAlerts()
        {
            return Ok(await _biReportService.GetLowStockAlertsAsync());
        }

        [HttpGet("monthly-trend")]
        public async Task<ActionResult<List<MonthlyTrendDto>>> GetMonthlyTrend()
        {
            return Ok(await _biReportService.GetMonthlyTrendDataAsync());
        }

        [HttpGet("shift-closure")]
        public async Task<ActionResult<ShiftClosureReportDto>> GetShiftClosureReport([FromQuery] int? sessionId, [FromQuery] DateTime? date)
        {
            return Ok(await _biReportService.GetShiftClosureReportAsync(sessionId, date));
        }

        [HttpGet("void-audit")]
        public async Task<ActionResult<List<VoidDiscountAuditRowDto>>> GetVoidDiscountAuditReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetVoidDiscountAuditReportAsync(from, to));
        }

        [HttpGet("staff-performance")]
        public async Task<ActionResult<List<StaffPerformanceReportDto>>> GetStaffPerformanceReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetStaffPerformanceReportAsync(from, to));
        }

        [HttpGet("stock-valuation")]
        public async Task<ActionResult<StockValuationSummaryDto>> GetStockValuationReport()
        {
            return Ok(await _biReportService.GetStockValuationReportAsync());
        }

        [HttpGet("pnl")]
        public async Task<ActionResult<ProfitAndLossReportDto>> GetProfitAndLossReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _biReportService.GetProfitAndLossReportAsync(from, to));
        }
    }

    public sealed class PurchaseReportQueryRequest
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 20;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? SupplierId { get; set; }
        public string? ItemName { get; set; }
        public string? PaymentType { get; set; }
        public string? InvoiceNo { get; set; }
    }

    public sealed class PagedPurchaseReportResponse
    {
        public List<PurchaseReportRowDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TotalQty { get; set; }
    }

    public sealed class LogWastageRequest
    {
        public int? ItemId { get; set; }
        public int? Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
