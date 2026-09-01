using HotelPOS.Application.DTOs.Report;
namespace HotelPOS.Application.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Returns aggregated sales data.
        /// Pass <paramref name="from"/> / <paramref name="to"/> (local time) to restrict the window.
        /// Null means no lower / upper bound.
        /// </summary>
        Task<SalesReportDto> GetSalesReportAsync(DateTime? from = null, DateTime? to = null);
        Task<SalesReportDto> GetSalesReportInternalAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Returns per-item performance, sorted by revenue descending.</summary>
        Task<List<ItemReportRowDto>> GetItemReportAsync(DateTime? from = null, DateTime? to = null);
        Task<List<ItemReportRowDto>> GetItemReportInternalAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Returns aggregated GST/revenue ledger data per day.</summary>
        Task<List<LedgerReportRowDto>> GetLedgerReportAsync(DateTime from, DateTime to);
        Task<List<LedgerReportRowDto>> GetLedgerReportInternalAsync(DateTime from, DateTime to);

        /// <summary>Returns the GSTR-1 filing report (B2B invoice-wise, B2C(Small) rate-wise
        /// summary, and HSN-wise summary) for the given period.</summary>
        Task<GstR1ReportDto> GetGstR1ReportAsync(DateTime from, DateTime to);
        Task<GstR1ReportDto> GetGstR1ReportInternalAsync(DateTime from, DateTime to);

        /// <summary>Returns monthly sales revenue for the current year.</summary>
        Task<List<MonthlySalesChartDto>> GetMonthlyChartDataAsync();
        Task<List<MonthlySalesChartDto>> GetMonthlyChartDataInternalAsync();

        /// <summary>Returns paged purchase report with totals.</summary>
        Task<(List<PurchaseReportRowDto> items, int totalCount, decimal totalPurchases, decimal totalTax, decimal totalDiscount, int totalQty)> GetPagedPurchaseReportAsync(PagedPurchaseReportRequest request);
        Task<(List<PurchaseReportRowDto> items, int totalCount, decimal totalPurchases, decimal totalTax, decimal totalDiscount, int totalQty)> GetPagedPurchaseReportInternalAsync(PagedPurchaseReportRequest request);
    }

    public record PagedPurchaseReportRequest(
        int Page,
        int PageSize,
        DateTime? From = null,
        DateTime? To = null,
        int? SupplierId = null,
        string? ItemName = null,
        string? PaymentType = null,
        string? InvoiceNo = null
    );
}
