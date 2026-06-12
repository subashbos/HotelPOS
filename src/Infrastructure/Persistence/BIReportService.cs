using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
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
                Math.Round(marginPercentage, 2),
                Math.Round(foodCostPercentage, 2)
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
                    if (margin < 10)
                        rec = "Critical: Increase price or negotiate cost.";
                    else if (margin < 25)
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
                        Math.Round(margin, 2),
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
                        Math.Round(pct, 2)
                    );
                })
                .OrderByDescending(r => r.Cost)
                .ToList();

            var recent = entries.OrderByDescending(e => e.WastedAt)
                .Take(50)
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
            var last30Days = DateTime.UtcNow.AddDays(-30);
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
                double dailyRate = Math.Round((double)sold / 30.0, 3);

                int daysRemaining = -1;
                if (item.StockQuantity <= 0)
                {
                    daysRemaining = 0;
                }
                else if (dailyRate > 0)
                {
                    daysRemaining = (int)Math.Ceiling(item.StockQuantity / dailyRate);
                }

                string alertLevel = "Normal";
                if (item.StockQuantity <= 0 || (daysRemaining >= 0 && daysRemaining <= 2))
                {
                    alertLevel = "Critical";
                }
                else if (item.StockQuantity <= item.MinStockThreshold || (daysRemaining >= 0 && daysRemaining <= 7))
                {
                    alertLevel = "Warning";
                }

                alerts.Add(new LowStockAlertDto(
                    idx++,
                    item.Id,
                    item.Name,
                    item.StockQuantity,
                    item.MinStockThreshold,
                    dailyRate,
                    daysRemaining,
                    alertLevel
                ));
            }

            return alerts.OrderBy(a => a.AlertLevel == "Critical" ? 0 : a.AlertLevel == "Warning" ? 1 : 2)
                .ThenBy(a => a.DaysRemaining >= 0 ? a.DaysRemaining : int.MaxValue)
                .Select((a, i) => a with { SNo = i + 1 })
                .ToList();
        }

        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendDataAsync()
        {
            var now = DateTime.Now;
            var startDateLocal = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            var startDateUtc = startDateLocal.ToUniversalTime();

            var orders = await _context.Orders.Include(o => o.Items)
                .Where(o => o.CreatedAt >= startDateUtc)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.Date >= startDateUtc)
                .ToListAsync();

            var itemsMap = await _context.Items.ToDictionaryAsync(i => i.Id, i => i);

            var result = new List<MonthlyTrendDto>();

            for (int i = 0; i < 12; i++)
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
    }
}
