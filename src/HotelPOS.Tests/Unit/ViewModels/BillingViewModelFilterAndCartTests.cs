using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingViewModelFilterAndCartTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();
        private readonly Mock<ITableService> _tableService = new();

        public BillingViewModelFilterAndCartTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });
        }

        [Fact]
        public void AddToCart_Calls_CartService_And_Updates_UI()
        {
            var vm = new BillingViewModel(
                _itemService.Object, _cartService.Object, _orderService.Object, _settingService.Object,
                _categoryService.Object, _notificationService.Object, _cashService.Object, _tableService.Object);

            var item = new Item { Id = 1, Name = "Pizza", Price = 100 };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);

            vm.AddToCartCommand.Execute(item);

            _cartService.Verify(s => s.AddItem(It.IsAny<int>(), item), Times.Once);
            _notificationService.Verify(n => n.ShowInfo(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ApplyFilter_Filters_By_SearchText()
        {
            var vm = new BillingViewModel(
                _itemService.Object, _cartService.Object, _orderService.Object, _settingService.Object,
                _categoryService.Object, _notificationService.Object, _cashService.Object, _tableService.Object);

            var items = new List<Item> {
                new Item { Id = 1, Name = "Apple" },
                new Item { Id = 2, Name = "Banana" }
            };

            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());

            await vm.InitializeAsync();
            vm.SearchText = "app";

            Assert.Single(vm.Items);
            Assert.Equal("Apple", vm.Items[0].Name);
        }
    }
}
