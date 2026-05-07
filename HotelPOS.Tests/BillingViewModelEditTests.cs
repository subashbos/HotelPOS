using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Tests for the BillingViewModel update (edit) flow:
    /// LoadOrderForEdit, CancelEdit, SaveOrder in edit mode.
    /// </summary>
    public class BillingViewModelEditTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();

        private readonly BillingViewModel _vm;

        public BillingViewModelEditTests()
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

        // ========== LoadOrderForEdit ===========

        [Fact]
        public void LoadOrderForEdit_SetsEditModeTrue()
        {
            var order = MakeOrder(42);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            _vm.LoadOrderForEdit(order);

            Assert.True(_vm.IsEditMode);
        }

        [Fact]
        public void LoadOrderForEdit_SetsTableNumber()
        {
            var order = MakeOrder(id: 1, tableNumber: 7);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            _vm.LoadOrderForEdit(order);

            Assert.Equal(7, _vm.TableNumber);
        }

        [Fact]
        public void LoadOrderForEdit_SetsDiscountPaymentAndCustomerFields()
        {
            var order = MakeOrder(1);
            order.DiscountAmount = 50m;
            order.PaymentMode = "UPI";
            order.CustomerName = "Ravi";
            order.CustomerPhone = "9876543210";
            order.CustomerGstin = "GSTIN123";
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            _vm.LoadOrderForEdit(order);

            Assert.Equal(50m, _vm.DiscountAmount);
            Assert.Equal("UPI", _vm.PaymentMode);
            Assert.Equal("Ravi", _vm.CustomerName);
            Assert.Equal("9876543210", _vm.CustomerPhone);
            Assert.Equal("GSTIN123", _vm.CustomerGstin);
        }

        [Fact]
        public void LoadOrderForEdit_CallsLoadItemsOnCartService()
        {
            var order = MakeOrder(5, tableNumber: 3);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            _vm.LoadOrderForEdit(order);

            _cartService.Verify(s => s.LoadItems(3, order.Items), Times.Once);
        }

        [Fact]
        public void LoadOrderForEdit_StatusMessageContainsOrderId()
        {
            var order = MakeOrder(99);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            _vm.LoadOrderForEdit(order);

            Assert.Contains("99", _vm.StatusMessage);
        }

        // ========== CancelEdit ===========

        [Fact]
        public void CancelEdit_ResetsEditModeToFalse()
        {
            var order = MakeOrder(10);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _vm.LoadOrderForEdit(order);

            _vm.CancelEditCommand.Execute(null);

            Assert.False(_vm.IsEditMode);
        }

        [Fact]
        public void CancelEdit_ClearsCartForOriginalTable()
        {
            var order = MakeOrder(id: 10, tableNumber: 4);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _vm.LoadOrderForEdit(order);

            _vm.CancelEditCommand.Execute(null);

            _cartService.Verify(s => s.Clear(4), Times.AtLeastOnce);
        }

        [Fact]
        public void CancelEdit_ResetsStatusMessageToReady()
        {
            var order = MakeOrder(11);
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _vm.LoadOrderForEdit(order);

            _vm.CancelEditCommand.Execute(null);

            Assert.Equal("Ready", _vm.StatusMessage);
        }

        // ========== SaveOrderAsync — edit mode (update path) ===========

        [Fact]
        public async Task SaveOrderAsync_EditMode_CallsUpdateOrderAsync()
        {
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Burger", Quantity = 1, Price = 100, Total = 100 }
            };
            var order = MakeOrder(20, tableNumber: 2);
            order.Items = orderItems;

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(orderItems);
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _orderService.Setup(s => s.GetAllOrdersWithItemsAsync()).ReturnsAsync(new List<Order>());

            _vm.LoadOrderForEdit(order);
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            _orderService.Verify(s => s.UpdateOrderAsync(It.Is<Order>(o => o.Id == 20)), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(
                It.IsAny<List<OrderItem>>(), It.IsAny<int>(), It.IsAny<decimal>(),
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_ShowsSuccessNotification()
        {
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 2, Price = 30, Total = 60 }
            };
            var order = MakeOrder(21);
            order.Items = orderItems;

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(orderItems);
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(60m);
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _orderService.Setup(s => s.GetAllOrdersWithItemsAsync()).ReturnsAsync(new List<Order>());

            _vm.LoadOrderForEdit(order);
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            _notificationService.Verify(
                n => n.ShowSuccess(It.Is<string>(s => s.Contains("21"))), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_ResetsEditModeAfterSave()
        {
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Juice", Quantity = 1, Price = 80, Total = 80 }
            };
            var order = MakeOrder(22);
            order.Items = orderItems;

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(orderItems);
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(80m);
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _orderService.Setup(s => s.GetAllOrdersWithItemsAsync()).ReturnsAsync(new List<Order>());

            _vm.LoadOrderForEdit(order);
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.False(_vm.IsEditMode);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_ResetsDiscountAndPaymentAfterSave()
        {
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Paneer", Quantity = 1, Price = 200, Total = 200 }
            };
            var order = MakeOrder(23);
            order.DiscountAmount = 25m;
            order.PaymentMode = "Card";
            order.Items = orderItems;

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(orderItems);
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(200m);
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _orderService.Setup(s => s.GetAllOrdersWithItemsAsync()).ReturnsAsync(new List<Order>());

            _vm.LoadOrderForEdit(order);
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            Assert.Equal(0m, _vm.DiscountAmount);
            Assert.Equal("Cash", _vm.PaymentMode);
        }

        [Fact]
        public async Task SaveOrderAsync_EditMode_UpdateFails_ShowsErrorNotification()
        {
            var orderItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Dosa", Quantity = 1, Price = 60, Total = 60 }
            };
            var order = MakeOrder(24);
            order.Items = orderItems;

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(orderItems);
            _orderService
                .Setup(s => s.UpdateOrderAsync(It.IsAny<Order>()))
                .ThrowsAsync(new Exception("DB connection lost"));

            _vm.LoadOrderForEdit(order);
            await _vm.SaveOrderCommand.ExecuteAsync(null);

            _notificationService.Verify(
                n => n.ShowError(It.Is<string>(s => s.Contains("DB connection lost"))), Times.Once);
            Assert.True(_vm.IsEditMode); // stays in edit mode on failure
        }

        // ========== Helper ===========

        private static Order MakeOrder(int id, int tableNumber = 1) => new Order
        {
            Id = id,
            TableNumber = tableNumber,
            Items = new List<OrderItem>(),
            DiscountAmount = 0,
            PaymentMode = "Cash"
        };
    }
}
