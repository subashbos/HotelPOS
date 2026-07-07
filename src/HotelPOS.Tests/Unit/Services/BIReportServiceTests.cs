using HotelPOS.Domain.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
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
    }
}

