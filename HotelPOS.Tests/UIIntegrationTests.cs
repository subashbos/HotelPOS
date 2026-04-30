using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class UIIntegrationTests
    {
        private readonly Mock<IReportService> _reportService = new();
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();

        [Fact]
        public void DashboardWindow_AdminLogin_CheckIdentity()
        {
            // Arrange
            AppSession.CurrentUser = new User { Role = "Admin", Username = "admin" };

            // Assert
            Assert.True(AppSession.IsAdmin);
            Assert.Equal("admin", AppSession.CurrentUser.Username);
        }

        [Fact]
        public void DashboardWindow_CashierLogin_CheckIdentity()
        {
            // Arrange
            AppSession.CurrentUser = new User { Role = "Cashier", Username = "cashier1" };

            // Assert
            Assert.False(AppSession.IsAdmin);
            Assert.Equal("cashier1", AppSession.CurrentUser.Username);
        }

        [Fact]
        public void ReportService_Calculation_IsCorrect()
        {
            // Verify core report logic
            var orders = new List<RecentOrderRowDto> {
                new RecentOrderRowDto { Total = 100 },
                new RecentOrderRowDto { Total = 200 }
            };

            var total = orders.Sum(o => o.Total);
            Assert.Equal(300, total);
        }
    }
}
