using HotelPOS.Domain.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using HotelPOS.Application.Interfaces;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Tests
{
    public class BIReportServiceTests
    {
        private HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new HotelDbContext(options);
        }

        [Fact]
        public async Task ProfitMarginSummary_CalculatesCOGSAndExpenses()
        {
            using var context = GetContext("BI_ProfitMarginDb");
            var service = new BIReportService(context);

            var item1 = new Item { Id = 1, Name = "Item A", Price = 100, CostPrice = 40, TrackInventory = true };
            var item2 = new Item { Id = 2, Name = "Item B", Price = 200, CostPrice = 120, TrackInventory = true };
            context.Items.AddRange(item1, item2);

            var order = new Order
            {
                Id = 1,
                InvoiceNumber = "INV-001",
                FiscalYear = "2026-27",
                TotalAmount = 400,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 2, Price = 100, Total = 200 },
                    new OrderItem { ItemId = 2, ItemName = "Item B", Quantity = 1, Price = 200, Total = 200 }
                }
            };
            context.Orders.Add(order);

            var expense = new Expense { Id = 1, Title = "Utilities", Amount = 50, Date = DateTime.UtcNow };
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();

            var summary = await service.GetProfitMarginSummaryAsync();

            Assert.Equal(400, summary.TotalRevenue);
            Assert.Equal(200, summary.TotalCogs); // 2 * 40 + 1 * 120
            Assert.Equal(200, summary.GrossProfit);
            Assert.Equal(50, summary.TotalExpenses);
            Assert.Equal(150, summary.NetProfit);
            Assert.Equal(50.0, summary.MarginPercentage);
            Assert.Equal(50.0, summary.FoodCostPercentage);
        }

        [Fact]
        public async Task LogWastage_DeductsInventoryCorrectly()
        {
            using var context = GetContext("BI_WastageDb");
            var service = new BIReportService(context);

            var item = new Item { Id = 10, Name = "Burger", StockQuantity = 100, TrackInventory = true, CostPrice = 50 };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            await service.LogWastageAsync(10, 15, "Spoilage", "Expired patties");

            var updatedItem = await context.Items.FindAsync(10);
            Assert.NotNull(updatedItem);
            Assert.Equal(85, updatedItem.StockQuantity);

            var wastage = await context.WastageEntries.FirstOrDefaultAsync();
            Assert.NotNull(wastage);
            Assert.Equal(10, wastage.ItemId);
            Assert.Equal(15, wastage.Quantity);
            Assert.Equal(50, wastage.CostPerUnit);
            Assert.Equal("Spoilage", wastage.Reason);
            Assert.Equal("Expired patties", wastage.Notes);
        }

        [Fact]
        public async Task LogWastage_FallsBackToPriceIfCostPriceIsZero()
        {
            using var context = GetContext("BI_WastageFallbackDb");
            var service = new BIReportService(context);

            var item = new Item { Id = 20, Name = "Porotta", StockQuantity = 100, TrackInventory = true, CostPrice = 0, Price = 100 };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            await service.LogWastageAsync(20, 5, "Spoilage", "Wasted");

            var wastage = await context.WastageEntries.FirstOrDefaultAsync();
            Assert.NotNull(wastage);
            Assert.Equal(20, wastage.ItemId);
            Assert.Equal(5, wastage.Quantity);
            Assert.Equal(100, wastage.CostPerUnit); // Base price fallback
        }

        [Fact]
        public async Task LowStockAlerts_ProvidesAlertLevels()
        {
            using var context = GetContext("BI_LowStockDb");
            var service = new BIReportService(context);

            var item1 = new Item { Id = 1, Name = "Item A", StockQuantity = 0, MinStockThreshold = 10, TrackInventory = true };
            var item2 = new Item { Id = 2, Name = "Item B", StockQuantity = 50, MinStockThreshold = 10, TrackInventory = true };
            context.Items.AddRange(item1, item2);
            await context.SaveChangesAsync();

            var alerts = await service.GetLowStockAlertsAsync();

            Assert.Equal(2, alerts.Count);
            
            var itemAAlert = alerts.First(a => a.ItemId == 1);
            Assert.Equal(AlertLevels.Critical, itemAAlert.AlertLevel); // Since 5 <= 10 (Warning/Critical depending on daily sales) and 5 is very low.

            var itemBAlert = alerts.First(a => a.ItemId == 2);
            Assert.Equal(AlertLevels.Normal, itemBAlert.AlertLevel);
        }

        [Fact]
        public async Task GetItemMarginsAsync_CalculatesCorrectly()
        {
            using var context = GetContext("BI_ItemMarginsDb");
            var service = new BIReportService(context);

            var category = new Category { Id = 1, Name = "Food" };
            context.Categories.Add(category);

            var item1 = new Item { Id = 1, Name = "Item A", Price = 100, CostPrice = 40, CategoryId = 1, Category = category };
            var item2 = new Item { Id = 2, Name = "Item B", Price = 200, CostPrice = 190, CategoryId = 1, Category = category }; // Critical margin (5%)
            context.Items.AddRange(item1, item2);

            var order = new Order
            {
                Id = 1,
                InvoiceNumber = "INV-001",
                FiscalYear = "2026-27",
                TotalAmount = 400,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 2, Price = 100, Total = 200 },
                    new OrderItem { ItemId = 2, ItemName = "Item B", Quantity = 1, Price = 200, Total = 200 }
                }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var margins = await service.GetItemMarginsAsync();

            Assert.Equal(2, margins.Count);
            
            var itemA = margins.First(x => x.ItemName == "Item A");
            Assert.Equal(60.0, itemA.MarginPercentage); // 200 rev - 80 cogs = 120 profit. 120/200 * 100 = 60%
            Assert.Contains("Healthy", itemA.Recommendation);

            var itemB = margins.First(x => x.ItemName == "Item B");
            Assert.Equal(5.0, itemB.MarginPercentage); // 200 rev - 190 cogs = 10 profit. 10/200 * 100 = 5%
            Assert.Contains("Critical", itemB.Recommendation);
        }

        [Fact]
        public async Task GetWastageSummaryAsync_AggregatesAndFiltersCorrectly()
        {
            using var context = GetContext("BI_WastageSummaryDb");
            var service = new BIReportService(context);

            var item = new Item { Id = 1, Name = "Milk", Price = 50, CostPrice = 40 };
            context.Items.Add(item);

            var w1 = new WastageEntry { Id = 1, ItemId = 1, Item = item, Quantity = 5, CostPerUnit = 40, Reason = "Spoilage", WastedAt = DateTime.UtcNow.AddDays(-1) };
            var w2 = new WastageEntry { Id = 2, ItemId = 1, Item = item, Quantity = 2, CostPerUnit = 40, Reason = "Spoilage", WastedAt = DateTime.UtcNow };
            var w3 = new WastageEntry { Id = 3, ItemId = 1, Item = item, Quantity = 3, CostPerUnit = 40, Reason = "Theft", WastedAt = DateTime.UtcNow };
            context.WastageEntries.AddRange(w1, w2, w3);
            await context.SaveChangesAsync();

            var summary = await service.GetWastageSummaryAsync();

            Assert.Equal(400, summary.TotalWastageCost); // (5+2+3) * 40 = 400
            Assert.Equal(10, summary.TotalWastageQty); // 5 + 2 + 3 = 10
            Assert.Equal(2, summary.ReasonsBreakdown.Count);

            var spoilageReason = summary.ReasonsBreakdown.First(r => r.Reason == "Spoilage");
            Assert.Equal(7, spoilageReason.Quantity); // 5 + 2 = 7
            Assert.Equal(280m, spoilageReason.Cost); // 7 * 40 = 280
            Assert.Equal(70.0, spoilageReason.Percentage); // 280/400 = 70%

            Assert.Equal(3, summary.RecentWastage.Count);
        }

        [Fact]
        public async Task GetMonthlyTrendDataAsync_CollectsTrailingMonthsCorrectly()
        {
            using var context = GetContext("BI_MonthlyTrendsDb");
            var service = new BIReportService(context);

            var item = new Item { Id = 1, Name = "Item A", Price = 100, CostPrice = 50 };
            context.Items.Add(item);

            var now = DateTime.UtcNow;

            var order = new Order
            {
                Id = 1,
                InvoiceNumber = "INV-001",
                FiscalYear = "2026-27",
                TotalAmount = 200,
                CreatedAt = now,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Item A", Quantity = 2, Price = 100, Total = 200 }
                }
            };
            context.Orders.Add(order);

            var expense = new Expense { Id = 1, Title = "General", Amount = 30, Date = now };
            context.Expenses.Add(expense);
            await context.SaveChangesAsync();

            var trends = await service.GetMonthlyTrendDataAsync();

            Assert.Equal(12, trends.Count);

            var currentMonthLabel = now.ToLocalTime().ToString("MMM yy");
            var currentMonthTrend = trends.First(t => t.MonthName == currentMonthLabel);
            
            Assert.Equal(200m, currentMonthTrend.Revenue);
            Assert.Equal(100m, currentMonthTrend.GrossProfit); // 200 rev - 100 cogs (2 * 50) = 100
            Assert.Equal(70m, currentMonthTrend.NetProfit); // 100 gross - 30 expense = 70
        }
    }
}

