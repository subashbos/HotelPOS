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

    public record ShiftClosureReportDto(
        int SessionId,
        DateTime OpenedAt,
        DateTime? ClosedAt,
        string OpenedBy,
        string? ClosedBy,
        string Status,
        decimal OpeningBalance,
        decimal TheoreticalClosingBalance,
        decimal ActualCashCounted,
        decimal CashVariance,
        decimal TotalSales,
        decimal CashSales,
        decimal CardSales,
        decimal UpiSales,
        decimal CreditSales,
        int TotalOrdersCount
    );

    public record VoidDiscountAuditRowDto(
        int SNo,
        int OrderId,
        string InvoiceNumber,
        DateTime Timestamp,
        string OrderType,
        decimal Subtotal,
        decimal DiscountAmount,
        decimal TotalAmount,
        string Status,
        string? RefundReason,
        string? VoidReason,
        string CustomerOrCashierInfo
    );

    public record StaffPerformanceReportDto(
        int SNo,
        int StaffId,
        string StaffName,
        string Role,
        int OrdersProcessedCount,
        decimal TotalRevenueGenerated,
        decimal AverageBillAmount,
        decimal TotalDiscountsGiven
    );

    public record StockValuationRowDto(
        int SNo,
        int ItemId,
        string ItemName,
        string CategoryName,
        int StockQuantity,
        decimal CostPrice,
        decimal RetailPrice,
        decimal TotalCostValue,
        decimal TotalRetailValue,
        string AbcCategory
    );

    public record StockValuationSummaryDto(
        decimal TotalInventoryCostValue,
        decimal TotalInventoryRetailValue,
        int TotalTrackedItemsCount,
        int HighValueCategoryACount,
        int MediumValueCategoryBCount,
        int LowValueCategoryCCount,
        List<StockValuationRowDto> Items
    );

    public record ExpenseCategoryBreakdownDto(
        int SNo,
        string Category,
        decimal Amount,
        double PercentageOfTotalExpenses
    );

    public record ProfitAndLossReportDto(
        DateTime PeriodFrom,
        DateTime PeriodTo,
        decimal TotalSalesRevenue,
        decimal TotalCostOfGoodsSold,
        decimal GrossProfit,
        double GrossProfitMarginPercentage,
        decimal TotalExpenses,
        List<ExpenseCategoryBreakdownDto> ExpensesByCategory,
        decimal NetOperatingProfit,
        double NetProfitMarginPercentage
    );

    public interface IBIReportService
    {
        Task<ProfitMarginSummaryDto> GetProfitMarginSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task<List<ItemMarginRowDto>> GetItemMarginsAsync(DateTime? from = null, DateTime? to = null);
        Task<WastageSummaryDto> GetWastageSummaryAsync(DateTime? from = null, DateTime? to = null);
        Task LogWastageAsync(int itemId, int quantity, string reason, string? notes);
        Task<List<LowStockAlertDto>> GetLowStockAlertsAsync();
        Task<List<MonthlyTrendDto>> GetMonthlyTrendDataAsync();

        Task<ShiftClosureReportDto> GetShiftClosureReportAsync(int? sessionId = null, DateTime? date = null);
        Task<List<VoidDiscountAuditRowDto>> GetVoidDiscountAuditReportAsync(DateTime? from = null, DateTime? to = null);
        Task<List<StaffPerformanceReportDto>> GetStaffPerformanceReportAsync(DateTime? from = null, DateTime? to = null);
        Task<StockValuationSummaryDto> GetStockValuationReportAsync();
        Task<ProfitAndLossReportDto> GetProfitAndLossReportAsync(DateTime? from = null, DateTime? to = null);
    }
}
