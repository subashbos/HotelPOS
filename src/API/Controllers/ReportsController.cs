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

        [HttpGet("items")]
        public async Task<ActionResult<List<ItemReportRowDto>>> GetItemReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            return Ok(await _reportService.GetItemReportAsync(from, to));
        }

        [HttpGet("gst")]
        public async Task<ActionResult<List<GstReportRowDto>>> GetGstReport([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            return Ok(await _reportService.GetGstReportAsync(from, to));
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
