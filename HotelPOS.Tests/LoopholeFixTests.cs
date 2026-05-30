using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class LoopholeFixTests
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

        public LoopholeFixTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

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
        public async Task UpdateRow_CapsQuantity_ToAvailableStock()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Burger", StockQuantity = 10, TrackInventory = true };
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item> { item });
            await _vm.InitializeAsync();

            var row = new CartRow { ItemId = 1, Quantity = 50 }; // Attempting to set 50

            // Act
            _vm.UpdateRow(row);

            // Assert
            Assert.Equal(10, row.Quantity);
            _notificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("capped"))), Times.Once);
            Assert.Contains("Stock Limit", _vm.StatusMessage);
        }

        [Fact]
        public async Task SelectedQty_CapsQuantity_ToAvailableStock()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Pizza", StockQuantity = 5, TrackInventory = true };
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item> { item });
            await _vm.InitializeAsync();

            var row = new CartRow { ItemId = 1, Quantity = 2 };
            _vm.Cart.Add(row);
            _vm.SelectedCartRow = row;

            // Act
            _vm.SelectedQty = 10; // Attempting to set 10

            // Assert
            Assert.Equal(5, _vm.SelectedQty);
            Assert.Contains("Out of stock", _vm.StatusMessage);
            _cartService.Verify(s => s.SetQuantity(It.IsAny<int>(), 1, 5), Times.Once);
        }

        [Fact]
        public async Task SelectedQty_PreventsAdding_WhenStockZero()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Cola", StockQuantity = 0, TrackInventory = true };
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item> { item });
            await _vm.InitializeAsync();

            var row = new CartRow { ItemId = 1, Quantity = 0 }; // Should not even be in cart if stock 0, but testing the setter
            _vm.Cart.Add(row);
            _vm.SelectedCartRow = row;

            // Act
            _vm.SelectedQty = 1;

            // Assert
            Assert.Contains("CANNOT ADD", _vm.StatusMessage);
            _cartService.Verify(s => s.SetQuantity(It.IsAny<int>(), 1, It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void UpdateRow_PreventsNegativePrice()
        {
            // Arrange
            var row = new CartRow { ItemId = 1, Quantity = 1, Price = -100m };

            // Act
            _vm.UpdateRow(row);

            // Assert
            Assert.Equal(0m, row.Price);
            Assert.Contains("Price cannot be negative", _vm.StatusMessage);
        }
    }
}
