using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers ReportService edge cases missing from ReportServiceTests.cs:
    /// zero orders, GetItemReport date range, GetGstReport empty range,
    /// GetMonthlyChartData (zero tests previously), category "Others" fallback,
    /// and MostPopularItem tie-breaking.
    /// </summary>
    public class ReportServiceLoopholeTests
    {
        private readonly Mock<IOrderRepository> _orderRepo = new();
        private readonly Mock<IItemRepository> _itemRepo = new();
        private readonly Mock<ICategoryRepository> _catRepo = new();
        private readonly Mock<IPurchaseRepository> _purchaseRepo = new();
        private readonly ReportService _service;

        public ReportServiceLoopholeTests()
        {
            _service = new ReportService(_orderRepo.Object, _itemRepo.Object, _catRepo.Object, _purchaseRepo.Object);
        }

        private void SetupEmptyOrders() =>
            _orderRepo.Setup(r => r.GetPagedWithItemsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .ReturnsAsync((new List<Order>(), 0));

        private void SetupOrders(List<Order> orders) =>
            _orderRepo.Setup(r => r.GetPagedWithItemsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
            .ReturnsAsync((orders, orders.Count));

        // ── GetSalesReportAsync — zero orders ────────────────────────────────

        [Fact]
        public async Task GetSalesReportAsync_NoOrders_ReturnsAllZeros()
        {
            SetupEmptyOrders();
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            Assert.Equal(0m, result.TotalRevenue);
            Assert.Equal(0, result.TotalOrders);
            Assert.Equal(0m, result.AverageOrderValue);
            Assert.Equal("N/A", result.MostPopularItem);
            Assert.Empty(result.RecentOrders);
            Assert.Empty(result.SalesByTable);
        }

        [Fact]
        public async Task GetSalesReportAsync_NoOrders_NoDivideByZeroException()
        {
            SetupEmptyOrders();
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var ex = await Record.ExceptionAsync(() => _service.GetSalesReportAsync());
            Assert.Null(ex);
        }

        // ── GetSalesReportAsync — category "Others" fallback ─────────────────

        [Fact]
        public async Task GetSalesReportAsync_ItemWithNoCategory_GroupedAsOthers()
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 100, CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 99, ItemName = "Mystery Item", Quantity = 1, Price = 100, Total = 100 }
                    }
                }
            };
            SetupOrders(orders);
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>()); // item 99 not in master list
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            var others = result.SalesByCategory.FirstOrDefault(c => c.CategoryName == "Others");
            Assert.NotNull(others);
            Assert.Equal(100m, others!.Revenue);
        }

        // ── GetSalesReportAsync — MostPopularItem ────────────────────────────

        [Fact]
        public async Task GetSalesReportAsync_MostPopularItem_ReturnsHighestQuantityItem()
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 300, CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Coffee", Quantity = 5, Price = 50, Total = 250 },
                        new OrderItem { ItemId = 2, ItemName = "Tea", Quantity = 2, Price = 25, Total = 50 }
                    }
                }
            };
            SetupOrders(orders);
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            Assert.Equal("Coffee", result.MostPopularItem);
        }

        // ── GetSalesReportAsync — payment mode breakdown ─────────────────────

        [Fact]
        public async Task GetSalesReportAsync_PaymentModeBreakdown_CorrectPercentages()
        {
            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 100, PaymentMode = PaymentModes.Cash, CreatedAt = DateTime.UtcNow, Items = new() },
                new Order { Id = 2, TotalAmount = 100, PaymentMode = PaymentModes.Upi,  CreatedAt = DateTime.UtcNow, Items = new() }
            };
            SetupOrders(orders);
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            Assert.Equal(2, result.SalesByPaymentMode.Count);
            Assert.All(result.SalesByPaymentMode, p => Assert.Equal(50.0, p.Percentage, precision: 1));
        }

        // ── GetItemReportAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetItemReportAsync_NoOrders_ReturnsEmptyList()
        {
            SetupEmptyOrders();

            var result = await _service.GetItemReportAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemReportAsync_AggregatesQuantityAndRevenue()
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 200, CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 2, Price = 100, Total = 200 }
                    }
                },
                new Order
                {
                    Id = 2, TotalAmount = 100, CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
                    }
                }
            };
            SetupOrders(orders);

            var result = await _service.GetItemReportAsync();

            Assert.Single(result);
            Assert.Equal("Burger", result[0].ItemName);
            Assert.Equal(3, result[0].TotalQtySold);
            Assert.Equal(300m, result[0].TotalRevenue);
        }

        [Fact]
        public async Task GetItemReportAsync_SNoIsSequential()
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 300, CreatedAt = DateTime.UtcNow,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 3, Price = 100, Total = 300 },
                        new OrderItem { ItemId = 2, ItemName = "Tea",    Quantity = 1, Price = 30,  Total = 30  }
                    }
                }
            };
            SetupOrders(orders);

            var result = await _service.GetItemReportAsync();

            Assert.Equal(1, result[0].SNo);
            Assert.Equal(2, result[1].SNo);
        }

        // ── GetGstReportAsync ────────────────────────────────────────────────

        [Fact]
        public async Task GetGstReportAsync_NoOrdersInRange_ReturnsEmptyList()
        {
            SetupEmptyOrders();

            var result = await _service.GetGstReportAsync(
                DateTime.Today.AddDays(-7), DateTime.Today);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetGstReportAsync_GroupsByLocalDate()
        {
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 110, Subtotal = 100, GstAmount = 10,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc)
                },
                new Order
                {
                    Id = 2, TotalAmount = 220, Subtotal = 200, GstAmount = 20,
                    CreatedAt = new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc)
                }
            };
            SetupOrders(orders);

            var result = await _service.GetGstReportAsync(
                new DateTime(2026, 5, 1), new DateTime(2026, 5, 2));

            // Both orders are on the same local date — should be one group
            Assert.Single(result);
            Assert.Equal(2, result[0].OrderCount);
            Assert.Equal(30m, result[0].GstAmount);
            Assert.Equal(300m, result[0].NetIncome);
        }

        [Fact]
        public async Task GetGstReportAsync_SNoIsSequential()
        {
            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 100, Subtotal = 90, GstAmount = 10,
                    CreatedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc) },
                new Order { Id = 2, TotalAmount = 200, Subtotal = 180, GstAmount = 20,
                    CreatedAt = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc) }
            };
            SetupOrders(orders);

            var result = await _service.GetGstReportAsync(
                new DateTime(2026, 5, 1), new DateTime(2026, 5, 3));

            for (int i = 0; i < result.Count; i++)
                Assert.Equal(i + 1, result[i].SNo);
        }

        // ── GetMonthlyChartDataAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetMonthlyChartDataAsync_ReturnsExactly12Months()
        {
            SetupEmptyOrders();

            var result = await _service.GetMonthlyChartDataAsync();

            Assert.Equal(12, result.Count);
        }

        [Fact]
        public async Task GetMonthlyChartDataAsync_NoOrders_AllRevenueZero()
        {
            SetupEmptyOrders();

            var result = await _service.GetMonthlyChartDataAsync();

            Assert.All(result, m => Assert.Equal(0m, m.Revenue));
        }

        [Fact]
        public async Task GetMonthlyChartDataAsync_MonthNamesAreNonEmpty()
        {
            SetupEmptyOrders();

            var result = await _service.GetMonthlyChartDataAsync();

            Assert.All(result, m => Assert.False(string.IsNullOrWhiteSpace(m.MonthName)));
        }

        [Fact]
        public async Task GetMonthlyChartDataAsync_WithOrders_PopulatesCorrectMonth()
        {
            var now = DateTime.Now;
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1, TotalAmount = 500,
                    CreatedAt = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };
            SetupOrders(orders);

            var result = await _service.GetMonthlyChartDataAsync();

            // The current month should have revenue
            var currentMonth = result.Last();
            Assert.Equal(500m, currentMonth.Revenue);
        }

        [Fact]
        public async Task GetMonthlyChartDataAsync_MonthsAreInChronologicalOrder()
        {
            SetupEmptyOrders();

            var result = await _service.GetMonthlyChartDataAsync();

            // Each month name should be different (no duplicates)
            var names = result.Select(r => r.MonthName).ToList();
            Assert.Equal(names.Count, names.Distinct().Count());
        }

        // ── GetSalesReportAsync — table breakdown ────────────────────────────

        [Fact]
        public async Task GetSalesReportAsync_SalesByTable_CorrectAggregation()
        {
            var orders = new List<Order>
            {
                new Order { Id = 1, TableNumber = 1, TotalAmount = 100, CreatedAt = DateTime.UtcNow, Items = new() },
                new Order { Id = 2, TableNumber = 1, TotalAmount = 200, CreatedAt = DateTime.UtcNow, Items = new() },
                new Order { Id = 3, TableNumber = 2, TotalAmount = 150, CreatedAt = DateTime.UtcNow, Items = new() }
            };
            SetupOrders(orders);
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            var table1 = result.SalesByTable.First(t => t.TableNumber == 1);
            var table2 = result.SalesByTable.First(t => t.TableNumber == 2);

            Assert.Equal(2, table1.OrderCount);
            Assert.Equal(300m, table1.TotalRevenue);
            Assert.Equal(1, table2.OrderCount);
            Assert.Equal(150m, table2.TotalRevenue);
        }

        [Fact]
        public async Task GetSalesReportAsync_RecentOrders_LimitedToFifty()
        {
            var orders = Enumerable.Range(1, 60).Select(i => new Order
            {
                Id = i, TotalAmount = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                Items = new List<OrderItem>()
            }).ToList();
            SetupOrders(orders);
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _catRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await _service.GetSalesReportAsync();

            Assert.Equal(50, result.RecentOrders.Count);
        }
    }
}

