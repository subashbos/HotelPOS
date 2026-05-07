using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BarcodeTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();

        public BarcodeTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetActiveTables()).Returns(new List<int>());
        }

        private BillingViewModel CreateViewModel()
        {
            return new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object
            );
        }

        [Fact]
        public async Task ApplyFilter_MatchesBarcode()
        {
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Burger", Barcode = "12345" },
                new Item { Id = 2, Name = "Pizza", Barcode = "67890" }
            };

            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());

            var vm = CreateViewModel();
            await vm.InitializeAsync();

            vm.SearchText = "67890";
            // OnSearchTextChanged calls ApplyFilter

            Assert.Single(vm.Items);
            Assert.Equal("Pizza", vm.Items[0].Name);
        }

        [Fact]
        public async Task ApplyFilter_MatchesPartialBarcode()
        {
            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item A", Barcode = "ABC123XYZ" }
            };

            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());

            var vm = CreateViewModel();
            await vm.InitializeAsync();

            vm.SearchText = "123";

            Assert.Single(vm.Items);
            Assert.Equal("Item A", vm.Items[0].Name);
        }
    }
}
