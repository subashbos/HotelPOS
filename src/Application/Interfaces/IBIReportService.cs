using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public record ProfitMarginSummaryDto(
        decimal TotalRevenue,
        decimal TotalCogs,
        decimal GrossProfit,
        decimal TotalExpenses,
        decimal NetProfit,
        double MarginPercentage,
        double FoodCostPercentage
    );

    public record ItemMarginRowDto(
        int SNo,
        string ItemName,
        string CategoryName,
        int QuantitySold,
        decimal UnitPrice,
        decimal CostPrice,
        decimal TotalRevenue,
        decimal TotalCogs,
        decimal Profit,
        double MarginPercentage,
        string Recommendation
    );

    public record WastageSummaryDto(
        decimal TotalWastageCost,
        int TotalWastageQty,
        List<WastageReasonRowDto> ReasonsBreakdown,
        List<WastageItemRowDto> RecentWastage
    );

    public record WastageReasonRowDto(
        int SNo,
        string Reason,
        int Quantity,
        decimal Cost,
        double Percentage
    );

    public record WastageItemRowDto(
        int SNo,
        int Id,
        string ItemName,
        int Quantity,
        string Reason,
        DateTime WastedAt,
        decimal TotalCost,
        string? Notes
    );

    public record LowStockAlertDto(
        int SNo,
        int ItemId,
        string ItemName,
        int CurrentStock,
        int MinThreshold,
        double DailyConsumptionRate,
        int DaysRemaining, // -1 means no sales/infinite, 0 means out of stock, positive means days
        string AlertLevel // "Critical", "Warning", "Normal"
    );

    public record MonthlyTrendDto(
        string MonthName,
        decimal Revenue,
        decimal GrossProfit,
        decimal NetProfit
    );

    public interface IBIReportService
    {
        Task<ProfitMarginSummaryDto> GetProfitMarginSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task<List<ItemMarginRowDto>> GetItemMarginsAsync(DateTime? from = null, DateTime? to = null);
        Task<WastageSummaryDto> GetWastageSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task LogWastageAsync(int itemId, int quantity, string reason, string? notes);
        Task<List<LowStockAlertDto>> GetLowStockAlertsAsync();
        Task<List<MonthlyTrendDto>> GetMonthlyTrendDataAsync();
    }
}
