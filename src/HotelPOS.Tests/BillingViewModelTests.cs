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
            _notificationService.Verify(n => n.ShowInfo(It.IsAny<string>()), Times.Never);
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

        [Fact]
        public async Task ToggleTransferMode_OpensPopup_WhenCartNotEmpty()
        {
            // Arrange
            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cartService.Setup(s => s.GetActiveTables()).Returns(new List<int>());
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession());

            // Act
            await _vm.InitializeAsync();
            _vm.ToggleTransferModeCommand.Execute(null);

            // Assert
            Assert.True(_vm.IsTransferMode);
            Assert.True(_vm.IsTableLayoutOpen);
            Assert.Equal("MOVE MODE: Select target table from the Table menu", _vm.StatusMessage);
        }

        [Fact]
        public async Task SelectTable_DuringTransfer_CallsTransfer_And_ResetsPopup()
        {
            // Arrange
            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cartService.Setup(s => s.GetActiveTables()).Returns(new List<int> { 1 });
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession());

            _vm.TableNumber = 1;
            await _vm.InitializeAsync();
            _vm.ToggleTransferModeCommand.Execute(null); // Now IsTransferMode = true, IsTableLayoutOpen = true

            // Act
            _vm.SelectTableCommand.Execute(5);

            // Assert
            _cartService.Verify(s => s.TransferTable(1, 5), Times.Once);
            Assert.False(_vm.IsTransferMode);
            Assert.False(_vm.IsTableLayoutOpen);
            Assert.Equal(5, _vm.TableNumber);
        }

        [Fact]
        public void DiscountAmount_Cannot_Be_Negative()
        {
            // Act
            _vm.DiscountAmount = -50m;

            // Assert
            Assert.Equal(0m, _vm.DiscountAmount);
            _notificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("negative"))), Times.Once);
        }

        [Fact]
        public void DiscountAmount_Cannot_Exceed_Subtotal()
        {
            // Arrange
            _cartService.Setup(s => s.GetSubtotal(2)).Returns(100m);
            _vm.TableNumber = 2; // Trigger UpdateCart to set subtotal

            // Act
            _vm.DiscountAmount = 150m;

            // Assert
            Assert.Equal(100m, _vm.DiscountAmount);
            _notificationService.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("exceed"))), Times.Once);
        }

        [Fact]
        public async Task AdjustQuantity_Checks_Stock_Before_Adding()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Burger", StockQuantity = 5, TrackInventory = true };
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item> { item });
            
            var row = new CartRow { ItemId = 1, Quantity = 5 };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 5 } });

            await _vm.InitializeAsync();

            // Act
            // Attempt to add 1 more when stock is already 5/5
            _vm.AdjustQuantityCommand.Execute(new object[] { row, 1 });

            // Assert
            _cartService.Verify(s => s.AddItem(It.IsAny<int>(), 1, 1), Times.Never);
            Assert.Contains("Out of stock", _vm.StatusMessage);
        }

        [Fact]
        public async Task HoldOrder_Clears_Cart_And_Adds_To_HeldOrders()
        {
            // Arrange
            var items = new List<CartRow> { new CartRow { ItemId = 1, Quantity = 1, ItemName = "Burger" } };
            _vm.Cart.Add(items[0]);
            
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } });
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder> { new HeldOrder { TableNumber = 1 } });

            // Act
            await _vm.HoldOrderCommand.ExecuteAsync(null);

            // Assert
            _cartService.Verify(s => s.HoldOrder(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
            _notificationService.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
            Assert.Single(_vm.HeldOrders);
        }

        [Fact]
        public async Task SaveOrderAsync_DialogConfirmed_SavesOrderSuccessfully()
        {
            // Arrange
            var mockDialogService = new Mock<IDialogService>();
            mockDialogService
                .Setup(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()))
                .ReturnsAsync(true); // User confirms!

            var localVm = new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object,
                _tableService.Object,
                mockDialogService.Object);

            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 100 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

            // Act
            await localVm.SaveOrderCommand.ExecuteAsync(null);

            // Assert
            mockDialogService.Verify(s => s.ShowConfirmCheckoutAsync(It.Is<ConfirmCheckoutDetails>(d => d.TotalItems == 2 && d.PaymentMode == "Cash")), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(
                It.IsAny<List<OrderItem>>(), It.IsAny<int>(), It.IsAny<decimal>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_DialogCancelled_AbortsCheckout()
        {
            // Arrange
            var mockDialogService = new Mock<IDialogService>();
            mockDialogService
                .Setup(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()))
                .ReturnsAsync(false); // User cancels!

            var localVm = new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object,
                _tableService.Object,
                mockDialogService.Object);

            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 100 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

            // Act
            await localVm.SaveOrderCommand.ExecuteAsync(null);

            // Assert
            mockDialogService.Verify(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(
                It.IsAny<List<OrderItem>>(), It.IsAny<int>(), It.IsAny<decimal>(), 
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
