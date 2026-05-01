using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        
        private readonly BillingViewModel _vm;

        public BillingViewModelTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _vm = new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object);
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

        [Fact]
        public void AddToCart_Calls_CartService_And_Updates_UI()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Pizza", Price = 100 };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);

            // Act
            _vm.AddToCartCommand.Execute(item);

            // Assert
            _cartService.Verify(s => s.AddItem(It.IsAny<int>(), item), Times.Once);
            _notificationService.Verify(n => n.ShowInfo(It.Is<string>(s => s.Contains("Pizza"))), Times.Once);
        }

        [Fact]
        public async Task ApplyFilter_Filters_By_SearchText()
        {
            // Arrange
            var items = new List<Item> { 
                new Item { Id = 1, Name = "Apple" },
                new Item { Id = 2, Name = "Banana" }
            };
            
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
            
            // Act
            await _vm.InitializeAsync();
            _vm.SearchText = "app"; // This triggers OnSearchTextChanged -> ApplyFilter

            // Assert
            Assert.Single(_vm.Items);
            Assert.Equal("Apple", _vm.Items[0].Name);
        }

        [Fact]
        public async Task SaveOrderAsync_EmptyCart_ShowsMessage()
        {
            // Arrange
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            // Act
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            // Assert
            _notificationService.Verify(n => n.ShowInfo(It.Is<string>(s => s.Contains("empty"))), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<List<OrderItem>>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
