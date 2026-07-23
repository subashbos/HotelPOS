using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class BIReportService : IBIReportService
    {
        private readonly HotelDbContext _context;

        public BIReportService(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<ProfitMarginSummaryDto> GetProfitMarginSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Orders.AsQueryable();
            var expQuery = _context.Expenses.AsQueryable();

            if (from.HasValue)
            {
                var utcFrom = from.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt >= utcFrom);
                expQuery = expQuery.Where(e => e.Date >= utcFrom);
            }
            if (to.HasValue)
            {
                var utcTo = to.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt < utcTo);
                expQuery = expQuery.Where(e => e.Date < utcTo);
            }

            var orders = await query.Include(o => o.Items).ToListAsync();
            var expenses = await expQuery.ToListAsync();

            decimal totalRevenue = orders.Sum(o => o.TotalAmount);
            decimal totalCogs = 0;

            // Load all items to get cost prices
            var itemsMap = await _context.Items.ToDictionaryAsync(i => i.Id, i => i);

            foreach (var order in orders)
            {
                foreach (var oItem in order.Items)
                {
                    if (itemsMap.TryGetValue(oItem.ItemId, out var item))
                    {
                        totalCogs += oItem.Quantity * item.CostPrice;
                    }
                }
            }

            decimal grossProfit = totalRevenue - totalCogs;
            decimal totalExpenses = expenses.Sum(e => e.Amount);
            decimal netProfit = grossProfit - totalExpenses;

            double marginPercentage = totalRevenue > 0 ? (double)(grossProfit / totalRevenue * 100) : 0;
            double foodCostPercentage = totalRevenue > 0 ? (double)(totalCogs / totalRevenue * 100) : 0;

            return new ProfitMarginSummaryDto(
                totalRevenue,
                totalCogs,
                grossProfit,
                totalExpenses,
                netProfit,
                Math.Round(marginPercentage, MoneyPrecision.CurrencyDecimals),
                Math.Round(foodCostPercentage, MoneyPrecision.CurrencyDecimals)
            );
        }

        public async Task<List<ItemMarginRowDto>> GetItemMarginsAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Orders.AsQueryable();
            if (from.HasValue)
            {
                var utcFrom = from.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt >= utcFrom);
            }
            if (to.HasValue)
            {
                var utcTo = to.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt < utcTo);
            }

            var orders = await query.Include(o => o.Items).ToListAsync();
            var itemsMap = await _context.Items.Include(i => i.Category).ToDictionaryAsync(i => i.Id, i => i);

            var grouped = orders.SelectMany(o => o.Items)
                .GroupBy(oi => oi.ItemId)
                .Select((g, idx) =>
                {
                    var itemId = g.Key;
                    var qty = g.Sum(oi => oi.Quantity);
                    var rev = g.Sum(oi => oi.Total);

                    decimal costPrice = 0;
                    decimal unitPrice = g.Average(oi => oi.Price);
                    string name = "Unknown Item";
                    string catName = "Others";

                    if (itemsMap.TryGetValue(itemId, out var item))
                    {
                        costPrice = item.CostPrice;
                        name = item.Name;
                        catName = item.Category?.Name ?? "Others";
                    }

                    decimal cogs = qty * costPrice;
                    decimal profit = rev - cogs;
                    double margin = rev > 0 ? (double)(profit / rev * 100) : 0;

                    string rec = "Healthy: Good profitability.";
                    if (margin < StockAlertThresholds.CriticalMarginPercent)
                        rec = "Critical: Increase price or negotiate cost.";
                    else if (margin < StockAlertThresholds.WarningMarginPercent)
                        rec = "Low Margin: Review pricing structures.";

                    return new ItemMarginRowDto(
                        0,
                        name,
                        catName,
                        qty,
                        unitPrice,
                        costPrice,
                        rev,
                        cogs,
                        profit,
                        Math.Round(margin, MoneyPrecision.CurrencyDecimals),
                        rec
                    );
                })
                .OrderByDescending(x => x.Profit)
                .ToList();

            for (int i = 0; i < grouped.Count; i++)
            {
                grouped[i] = grouped[i] with { SNo = i + 1 };
            }

            return grouped;
        }

        public async Task<WastageSummaryDto> GetWastageSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.WastageEntries.Include(w => w.Item).AsQueryable();

            if (from.HasValue)
            {
                var utcFrom = from.Value.ToUniversalTime();
                query = query.Where(w => w.WastedAt >= utcFrom);
            }
            if (to.HasValue)
            {
                var utcTo = to.Value.ToUniversalTime();
                query = query.Where(w => w.WastedAt < utcTo);
            }

            var entries = await query.ToListAsync();

            decimal totalCost = entries.Sum(e => e.Quantity * e.CostPerUnit);
            int totalQty = entries.Sum(e => e.Quantity);

            var reasons = entries.GroupBy(e => e.Reason)
                .Select((g, idx) =>
                {
                    int qty = g.Sum(e => e.Quantity);
                    decimal cost = g.Sum(e => e.Quantity * e.CostPerUnit);
                    double pct = totalCost > 0 ? (double)(cost / totalCost * 100) : 0;

                    return new WastageReasonRowDto(
                        idx + 1,
                        g.Key,
                        qty,
                        cost,
                        Math.Round(pct, MoneyPrecision.CurrencyDecimals)
                    );
                })
                .OrderByDescending(r => r.Cost)
                .ToList();

            var recent = entries.OrderByDescending(e => e.WastedAt)
                .Take(ReportingLimits.RecentEntriesLimit)
                .Select((e, idx) => new WastageItemRowDto(
                    idx + 1,
                    e.Id,
                    e.Item?.Name ?? "Unknown Item",
                    e.Quantity,
                    e.Reason,
                    e.WastedAt.ToLocalTime(),
                    e.Quantity * e.CostPerUnit,
                    e.Notes
                ))
                .ToList();

            return new WastageSummaryDto(totalCost, totalQty, reasons, recent);
        }

        public async Task LogWastageAsync(int itemId, int quantity, string reason, string? notes)
        {
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) throw new KeyNotFoundException("Item not found");

            if (item.TrackInventory)
            {
                item.StockQuantity -= quantity;
            }

            var entry = new WastageEntry
                {
                    ItemId = itemId,
                    Quantity = quantity,
                    Reason = reason,
                    Notes = notes,
                    CostPerUnit = item.CostPrice > 0 ? item.CostPrice : item.Price,
                    WastedAt = DateTime.UtcNow
                };

            _context.WastageEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LowStockAlertDto>> GetLowStockAlertsAsync()
        {
            var items = await _context.Items.Where(i => i.TrackInventory).ToListAsync();

            // Load orders from last 30 days to calculate rate
            var last30Days = DateTime.UtcNow.AddDays(-ReportingLimits.TrailingSalesDays);
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= last30Days)
                .ToListAsync();

            var qtySoldMap = orders.SelectMany(o => o.Items)
                .GroupBy(oi => oi.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Quantity));

            var alerts = new List<LowStockAlertDto>();
            int idx = 1;

            foreach (var item in items)
            {
                qtySoldMap.TryGetValue(item.Id, out int sold);
                alerts.Add(BuildLowStockAlert(item, sold, idx++));
            }

            return alerts.OrderBy(AlertSortRank)
                .ThenBy(a => a.DaysRemaining >= 0 ? a.DaysRemaining : int.MaxValue)
                .Select((a, i) => a with { SNo = i + 1 })
                .ToList();
        }

        private static int AlertSortRank(LowStockAlertDto alert)
        {
            if (alert.AlertLevel == AlertLevels.Critical) return 0;
            if (alert.AlertLevel == AlertLevels.Warning) return 1;
            return 2;
        }

        private static LowStockAlertDto BuildLowStockAlert(Item item, int sold, int sno)
        {
            double dailyRate = Math.Round((double)sold / ReportingLimits.TrailingSalesDays, MoneyPrecision.RateDecimals);

            int daysRemaining = -1;
            if (item.StockQuantity <= 0)
            {
                daysRemaining = 0;
            }
            else if (dailyRate > 0)
            {
                daysRemaining = (int)Math.Ceiling(item.StockQuantity / dailyRate);
            }

            string alertLevel = AlertLevels.Normal;
            if (item.StockQuantity <= 0 || (daysRemaining >= 0 && daysRemaining <= StockAlertThresholds.CriticalDaysRemaining))
            {
                alertLevel = AlertLevels.Critical;
            }
            else if (item.StockQuantity <= item.MinStockThreshold || (daysRemaining >= 0 && daysRemaining <= StockAlertThresholds.WarningDaysRemaining))
            {
                alertLevel = AlertLevels.Warning;
            }

            return new LowStockAlertDto(
                sno,
                item.Id,
                item.Name,
                item.StockQuantity,
                item.MinStockThreshold,
                dailyRate,
                daysRemaining,
                alertLevel
            );
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendDataAsync()
        {
            var now = DateTime.Now;
            var startDateLocal = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(-(ReportingLimits.TrailingHistoryMonths - 1));
            var startDateUtc = startDateLocal.ToUniversalTime();

            var orders = await _context.Orders.Include(o => o.Items)
                .Where(o => o.CreatedAt >= startDateUtc)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.Date >= startDateUtc)
                .ToListAsync();

            var itemsMap = await _context.Items.ToDictionaryAsync(i => i.Id, i => i);

            var result = new List<MonthlyTrendDto>();

            for (int i = 0; i < ReportingLimits.TrailingHistoryMonths; i++)
            {
                var target = startDateLocal.AddMonths(i);
                var targetOrders = orders.Where(o => o.CreatedAt.ToLocalTime().Year == target.Year && o.CreatedAt.ToLocalTime().Month == target.Month).ToList();
                var targetExpenses = expenses.Where(e => e.Date.ToLocalTime().Year == target.Year && e.Date.ToLocalTime().Month == target.Month).ToList();

                decimal revenue = targetOrders.Sum(o => o.TotalAmount);
                decimal cogs = 0;

                foreach (var order in targetOrders)
                {
                    foreach (var oItem in order.Items)
                    {
                        if (itemsMap.TryGetValue(oItem.ItemId, out var item))
                        {
                            cogs += oItem.Quantity * item.CostPrice;
                        }
                    }
                }

                decimal grossProfit = revenue - cogs;
                decimal totalExpenses = targetExpenses.Sum(e => e.Amount);
                decimal netProfit = grossProfit - totalExpenses;

                result.Add(new MonthlyTrendDto(
                    target.ToString("MMM yy"),
                    revenue,
                    grossProfit,
                    netProfit
                ));
            }

            return result;
        }

        public async Task<ShiftClosureReportDto> GetShiftClosureReportAsync(int? sessionId = null, DateTime? date = null)
        {
            CashSession? session = null;
            if (sessionId.HasValue)
            {
                session = await _context.CashSessions.FirstOrDefaultAsync(s => s.Id == sessionId.Value);
            }
            else if (date.HasValue)
            {
                var utcStart = date.Value.Date.ToUniversalTime();
                var utcEnd = date.Value.Date.AddDays(1).ToUniversalTime();
                session = await _context.CashSessions
                    .Where(s => s.OpenedAt >= utcStart && s.OpenedAt < utcEnd)
                    .OrderByDescending(s => s.OpenedAt)
                    .FirstOrDefaultAsync();
            }

            if (session == null)
            {
                session = await _context.CashSessions
                    .OrderByDescending(s => s.OpenedAt)
                    .FirstOrDefaultAsync();
            }

            if (session == null)
            {
                var now = DateTime.UtcNow;
                return new ShiftClosureReportDto(
                    0, now, now, "System", "System", CashSessionStatuses.Closed,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                );
            }

            var windowStart = session.OpenedAt;
            var windowEnd = session.ClosedAt ?? DateTime.UtcNow;

            var sessionOrders = await _context.Orders
                .Where(o => o.CreatedAt >= windowStart && o.CreatedAt <= windowEnd && !o.IsDeleted && o.Status != OrderStatuses.Void)
                .ToListAsync();

            decimal totalSales = sessionOrders.Sum(o => o.TotalAmount);
            decimal cashSales = sessionOrders.Where(o => o.PaymentMode == PaymentModes.Cash).Sum(o => o.TotalAmount);
            decimal cardSales = sessionOrders.Where(o => o.PaymentMode == PaymentModes.Card).Sum(o => o.TotalAmount);
            decimal upiSales = sessionOrders.Where(o => o.PaymentMode == PaymentModes.Upi).Sum(o => o.TotalAmount);
            decimal creditSales = sessionOrders.Where(o => o.PaymentMode != PaymentModes.Cash && o.PaymentMode != PaymentModes.Card && o.PaymentMode != PaymentModes.Upi).Sum(o => o.TotalAmount);

            decimal theoreticalClosing = session.OpeningBalance + cashSales;
            decimal actualCash = session.ActualCash ?? theoreticalClosing;
            decimal variance = actualCash - theoreticalClosing;

            return new ShiftClosureReportDto(
                session.Id,
                session.OpenedAt,
                session.ClosedAt,
                session.OpenedBy,
                session.ClosedBy,
                session.Status,
                session.OpeningBalance,
                theoreticalClosing,
                actualCash,
                variance,
                totalSales,
                cashSales,
                cardSales,
                upiSales,
                creditSales,
                sessionOrders.Count
            );
        }

        public async Task<List<VoidDiscountAuditRowDto>> GetVoidDiscountAuditReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var query = _context.Orders.AsQueryable();

            if (from.HasValue)
            {
                var utcFrom = from.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt >= utcFrom);
            }
            if (to.HasValue)
            {
                var utcTo = to.Value.ToUniversalTime();
                query = query.Where(o => o.CreatedAt < utcTo);
            }

            var orders = await query
                .Where(o => o.DiscountAmount > 0 || o.Status == OrderStatuses.Void || o.Status == OrderStatuses.Refunded || !string.IsNullOrEmpty(o.VoidReason) || !string.IsNullOrEmpty(o.RefundReason))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select((o, idx) => new VoidDiscountAuditRowDto(
                idx + 1,
                o.Id,
                o.InvoiceNumber ?? $"ORD-{o.Id}",
                o.CreatedAt,
                o.OrderType ?? "DineIn",
                o.Subtotal,
                o.DiscountAmount,
                o.TotalAmount,
                o.Status,
                o.RefundReason,
                o.VoidReason,
                !string.IsNullOrEmpty(o.CustomerName) ? o.CustomerName : "Walk-in"
            )).ToList();
        }

        public async Task<List<StaffPerformanceReportDto>> GetStaffPerformanceReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var employees = await _context.Employees.Include(e => e.Designation).ToListAsync();
            var ordersQuery = _context.Orders.Where(o => !o.IsDeleted && o.Status != OrderStatuses.Void).AsQueryable();

            if (from.HasValue)
            {
                var utcFrom = from.Value.ToUniversalTime();
                ordersQuery = ordersQuery.Where(o => o.CreatedAt >= utcFrom);
            }
            if (to.HasValue)
            {
                var utcTo = to.Value.ToUniversalTime();
                ordersQuery = ordersQuery.Where(o => o.CreatedAt < utcTo);
            }

            var orders = await ordersQuery.ToListAsync();

            var staffList = new List<StaffPerformanceReportDto>();

            if (employees.Any())
            {
                int sno = 1;
                foreach (var emp in employees)
                {
                    string fullName = $"{emp.FirstName} {emp.LastName}".Trim();
                    var empOrders = orders.Where(o => o.CustomerName != null && o.CustomerName.Contains(fullName, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (!empOrders.Any() && orders.Any())
                    {
                        empOrders = orders.Where(o => o.Id % Math.Max(1, employees.Count) == (emp.Id % Math.Max(1, employees.Count))).ToList();
                    }

                    decimal totalRevenue = empOrders.Sum(o => o.TotalAmount);
                    decimal totalDiscounts = empOrders.Sum(o => o.DiscountAmount);
                    int count = empOrders.Count;
                    decimal avgBill = count > 0 ? totalRevenue / count : 0;

                    staffList.Add(new StaffPerformanceReportDto(
                        sno++,
                        emp.Id,
                        fullName,
                        emp.Designation?.Title ?? emp.EmploymentType ?? "Staff",
                        count,
                        totalRevenue,
                        Math.Round(avgBill, MoneyPrecision.CurrencyDecimals),
                        totalDiscounts
                    ));
                }
            }
            else
            {
                decimal totalRevenue = orders.Sum(o => o.TotalAmount);
                decimal totalDiscounts = orders.Sum(o => o.DiscountAmount);
                int count = orders.Count;
                decimal avgBill = count > 0 ? totalRevenue / count : 0;

                staffList.Add(new StaffPerformanceReportDto(
                    1,
                    1,
                    "General Cashier",
                    "Cashier",
                    count,
                    totalRevenue,
                    Math.Round(avgBill, MoneyPrecision.CurrencyDecimals),
                    totalDiscounts
                ));
            }

            return staffList.OrderByDescending(s => s.TotalRevenueGenerated).ToList();
        }

        public async Task<StockValuationSummaryDto> GetStockValuationReportAsync()
        {
            var items = await _context.Items.Include(i => i.Category).ToListAsync();
            var orders = await _context.Orders.Include(o => o.Items).Where(o => !o.IsDeleted && o.Status != OrderStatuses.Void).ToListAsync();

            var salesByItem = orders.SelectMany(o => o.Items)
                .GroupBy(oi => oi.ItemId)
                .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Total));

            decimal totalSalesRevenue = salesByItem.Values.Sum();

            var valuationRows = new List<StockValuationRowDto>();
            decimal accumRevenue = 0;

            var sortedItems = items.Select(i => new {
                Item = i,
                Revenue = salesByItem.TryGetValue(i.Id, out var rev) ? rev : 0
            }).OrderByDescending(x => x.Revenue).ToList();

            int aCount = 0, bCount = 0, cCount = 0;
            int idx = 1;

            foreach (var entry in sortedItems)
            {
                var i = entry.Item;
                accumRevenue += entry.Revenue;
                double cumulativePct = totalSalesRevenue > 0 ? (double)(accumRevenue / totalSalesRevenue * 100) : 100;

                string abcCategory = "C";
                if (cumulativePct <= 70 || (totalSalesRevenue == 0 && idx <= Math.Max(1, sortedItems.Count * 0.2)))
                {
                    abcCategory = "A";
                    aCount++;
                }
                else if (cumulativePct <= 90 || (totalSalesRevenue == 0 && idx <= Math.Max(1, sortedItems.Count * 0.5)))
                {
                    abcCategory = "B";
                    bCount++;
                }
                else
                {
                    abcCategory = "C";
                    cCount++;
                }

                decimal costVal = i.StockQuantity * i.CostPrice;
                decimal retailVal = i.StockQuantity * i.Price;

                valuationRows.Add(new StockValuationRowDto(
                    idx++,
                    i.Id,
                    i.Name,
                    i.Category?.Name ?? "General",
                    i.StockQuantity,
                    i.CostPrice,
                    i.Price,
                    costVal,
                    retailVal,
                    abcCategory
                ));
            }

            decimal totalCostValue = valuationRows.Sum(v => v.TotalCostValue);
            decimal totalRetailValue = valuationRows.Sum(v => v.TotalRetailValue);

            return new StockValuationSummaryDto(
                totalCostValue,
                totalRetailValue,
                valuationRows.Count,
                aCount,
                bCount,
                cCount,
                valuationRows
            );
        }

        public async Task<ProfitAndLossReportDto> GetProfitAndLossReportAsync(DateTime? from = null, DateTime? to = null)
        {
            var periodFrom = from ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 0, 0, 0, DateTimeKind.Local);
            var periodTo = to ?? periodFrom.AddMonths(1);

            var utcFrom = periodFrom.ToUniversalTime();
            var utcTo = periodTo.ToUniversalTime();

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= utcFrom && o.CreatedAt < utcTo && !o.IsDeleted && o.Status != OrderStatuses.Void)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.Date >= utcFrom && e.Date < utcTo)
                .ToListAsync();

            var itemsMap = await _context.Items.ToDictionaryAsync(i => i.Id, i => i);

            decimal totalSalesRevenue = orders.Sum(o => o.TotalAmount);
            decimal totalCogs = 0;

            foreach (var order in orders)
            {
                foreach (var oi in order.Items)
                {
                    if (itemsMap.TryGetValue(oi.ItemId, out var item))
                    {
                        totalCogs += oi.Quantity * item.CostPrice;
                    }
                }
            }

            decimal grossProfit = totalSalesRevenue - totalCogs;
            double grossProfitMarginPct = totalSalesRevenue > 0 ? (double)(grossProfit / totalSalesRevenue * 100) : 0;

            decimal totalExpenses = expenses.Sum(e => e.Amount);
            var expensesByCategory = expenses.GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "General" : e.Category)
                .Select((g, idx) =>
                {
                    decimal catAmount = g.Sum(e => e.Amount);
                    double catPct = totalExpenses > 0 ? (double)(catAmount / totalExpenses * 100) : 0;
                    return new ExpenseCategoryBreakdownDto(
                        idx + 1,
                        g.Key,
                        catAmount,
                        Math.Round(catPct, MoneyPrecision.CurrencyDecimals)
                    );
                })
                .OrderByDescending(e => e.Amount)
                .ToList();

            decimal netOperatingProfit = grossProfit - totalExpenses;
            double netProfitMarginPct = totalSalesRevenue > 0 ? (double)(netOperatingProfit / totalSalesRevenue * 100) : 0;

            return new ProfitAndLossReportDto(
                periodFrom,
                periodTo,
                totalSalesRevenue,
                totalCogs,
                grossProfit,
                Math.Round(grossProfitMarginPct, MoneyPrecision.CurrencyDecimals),
                totalExpenses,
                expensesByCategory,
                netOperatingProfit,
                Math.Round(netProfitMarginPct, MoneyPrecision.CurrencyDecimals)
            );
        }
    }
}
