using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingViewModelTests
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

        public BillingViewModelTests()
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
        public async Task InitializeAsync_LoadsItemsAndCategories()
        {
            // Arrange
            var items = new List<Item> { new Item { Id = 1, Name = "Burger" } };
            var categories = new List<Category> { new Category { Id = 1, Name = "Fast Food" } };

            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(categories);
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession());

            // Act
            await _vm.InitializeAsync();

            // Assert
            Assert.Single(_vm.Items);
            Assert.Contains(_vm.Items, i => i.Name == "Burger");
            Assert.Equal(2, _vm.Categories.Count); // "All" + "Fast Food"
        }

    }
}

