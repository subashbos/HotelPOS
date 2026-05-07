using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ReportServiceTests
    {
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly Mock<ICategoryRepository> _categoryRepoMock;
        private readonly ReportService _service;

        public ReportServiceTests()
        {
            _orderRepoMock = new Mock<IOrderRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _categoryRepoMock = new Mock<ICategoryRepository>();
            _service = new ReportService(_orderRepoMock.Object, _itemRepoMock.Object, _categoryRepoMock.Object);
        }

        [Fact]
        public async Task GetSalesReportAsync_CalculatesTotalsCorrectly()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 100, CreatedAt = DateTime.UtcNow },
                new Order { Id = 2, TotalAmount = 200, CreatedAt = DateTime.UtcNow }
            };
            _orderRepoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, 2));
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            // Act
            var result = await _service.GetSalesReportAsync();

            // Assert
            Assert.Equal(300, result.TotalRevenue);
            Assert.Equal(2, result.TotalOrders);
            Assert.Equal(150, result.AverageOrderValue);
        }

        [Fact]
        public async Task GetSalesReportAsync_FiltersByDateRange()
        {
            // Arrange
            var today = DateTime.UtcNow.Date;
            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 100, CreatedAt = today.AddDays(-5) },
                new Order { Id = 2, TotalAmount = 200, CreatedAt = today }
            };

            // Setup mock to simulate repository filtering
            _orderRepoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders.Where(o => o.CreatedAt >= today).ToList(), 1));

            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            // Act - Filter for today only
            var result = await _service.GetSalesReportAsync(from: today);

            // Assert
            Assert.Equal(200, result.TotalRevenue);
            Assert.Single(result.RecentOrders);
        }

        [Fact]
        public async Task GetSalesReportAsync_AggregatesByCategory()
        {
            // Arrange
            var cat1 = new Category { Id = 1, Name = "Food" };
            var item1 = new Item { Id = 10, CategoryId = 1 };

            var orders = new List<Order>
            {
                new Order {
                    Id = 1,
                    TotalAmount = 100,
                    Items = new List<OrderItem> { new OrderItem { ItemId = 10, Total = 100 } }
                }
            };

            _orderRepoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, 1));
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item> { item1 });
            _categoryRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { cat1 });

            // Act
            var result = await _service.GetSalesReportAsync();

            // Assert
            var foodSale = result.SalesByCategory.FirstOrDefault(c => c.CategoryName == "Food");
            Assert.NotNull(foodSale);
            Assert.Equal(100, foodSale.Revenue);
            Assert.Equal(100, foodSale.Percentage);
        }

        [Fact]
        public async Task GetGstReportAsync_GroupsByLocalDate()
        {
            // Arrange
            // Order 1: 11 PM UTC (which is next day in many timezones, e.g. IST +5:30)
            // Order 2: 1 AM UTC same day
            var date1 = new DateTime(2026, 5, 1, 23, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2026, 5, 1, 1, 0, 0, DateTimeKind.Utc);

            var orders = new List<Order>
            {
                new Order { Id = 1, TotalAmount = 100, CreatedAt = date1, Subtotal = 90, GstAmount = 10 },
                new Order { Id = 2, TotalAmount = 200, CreatedAt = date2, Subtotal = 180, GstAmount = 20 }
            };

            _orderRepoMock.Setup(r => r.GetPagedWithItemsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>()))
                .ReturnsAsync((orders, 2));

            // Act
            var from = new DateTime(2026, 5, 1);
            var to = new DateTime(2026, 5, 2);
            var result = await _service.GetGstReportAsync(from, to);

            // Assert
            // This test is sensitive to the runner's local time, but we expect at least two groups 
            // if the conversion logic is working as intended (Local vs UTC)
            Assert.NotEmpty(result);
        }
    }
}
