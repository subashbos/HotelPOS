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

        /// <summary>Returns aggregated GST data per day.</summary>
        Task<List<GstReportRowDto>> GetGstReportAsync(DateTime from, DateTime to);
        Task<List<GstReportRowDto>> GetGstReportInternalAsync(DateTime from, DateTime to);

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
