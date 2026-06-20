using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingLoopholesTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();
        private readonly Mock<ITableService> _tableService = new();

        private readonly BillingViewModel _vm;

        public BillingLoopholesTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            
            _vm = new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object,
                _tableService.Object);
        }

        [Fact]
        public void SelectedQty_Enforces_Minimum_Of_1()
        {
            // Arrange
            var row = new CartRow { ItemId = 1, ItemName = "Test", Quantity = 2 };
            _vm.SelectedCartRow = row;

            // Act
            _vm.SelectedQty = -5;

            // Assert
            Assert.Equal(1, _vm.SelectedQty);
            _cartService.Verify(s => s.SetQuantity(It.IsAny<int>(), 1, 1), Times.Once());
            Assert.Equal("Quantity cannot be less than 1.", _vm.StatusMessage);
        }

        [Fact]
        public void UpdateRow_Prevents_Negative_Price()
        {
            // Arrange
            var row = new CartRow { ItemId = 1, ItemName = "Test", Price = -100m, Quantity = 1 };

            // Act
            _vm.UpdateRowCommand.Execute(row);

            // Assert
            Assert.Equal(0m, row.Price);
            _cartService.Verify(s => s.UpdatePrice(It.IsAny<int>(), 1, 0m), Times.Once);
            Assert.Equal("Price cannot be negative.", _vm.StatusMessage);
        }

        [Fact]
        public async Task SaveOrder_With_ClosedShift_Blocked()
        {
            // Arrange
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } });
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null); // Shift closed

            // Act
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            // Assert
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
            _notificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Shift is not open"))), Times.Once);
        }

        [Fact]
        public void UpdateRow_Enforces_Minimum_Quantity()
        {
            // Arrange
            var row = new CartRow { ItemId = 1, ItemName = "Test", Quantity = 0 };

            // Act
            _vm.UpdateRowCommand.Execute(row);

            // Assert
            Assert.Equal(1, row.Quantity);
            _cartService.Verify(s => s.SetQuantity(It.IsAny<int>(), 1, 1), Times.Once);
        }
    }
}

