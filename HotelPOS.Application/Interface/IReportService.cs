namespace HotelPOS.Application.Interface
{
    public interface IReportService
    {
        /// <summary>
        /// Returns aggregated sales data.
        /// Pass <paramref name="from"/> / <paramref name="to"/> (local time) to restrict the window.
        /// Null means no lower / upper bound.
        /// </summary>
        Task<SalesReportDto> GetSalesReportAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Returns per-item performance, sorted by revenue descending.</summary>
        Task<List<ItemReportRowDto>> GetItemReportAsync(DateTime? from = null, DateTime? to = null);

        /// <summary>Returns aggregated GST data per day.</summary>
        Task<List<GstReportRowDto>> GetGstReportAsync(DateTime from, DateTime to);

        /// <summary>Returns monthly sales revenue for the current year.</summary>
        Task<List<MonthlySalesChartDto>> GetMonthlyChartDataAsync();
    }
}
