using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingViewModelCheckoutTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();
        private readonly Mock<ITableService> _tableService = new();

        public BillingViewModelCheckoutTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });
        }

        [Fact]
        public async Task SaveOrderAsync_EmptyCart_ShowsMessage()
        {
            BillingViewModel.RegisterTestServices(
                _itemService.Object, _orderService.Object, _categoryService.Object,
                _cashService.Object, _tableService.Object);
            var vm = new BillingViewModel(
                _cartService.Object, _settingService.Object, _notificationService.Object);

            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

            await vm.SaveOrderCommand.ExecuteAsync(null);

            _notificationService.Verify(n => n.ShowInfo(It.Is<string>(s => s.Contains("empty"))), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }

        [Fact]
        public async Task SaveOrderAsync_DineInWithoutTable_ShowsErrorAndDoesNotSave()
        {
            BillingViewModel.RegisterTestServices(
                _itemService.Object, _orderService.Object, _categoryService.Object,
                _cashService.Object, _tableService.Object);
            var vm = new BillingViewModel(
                _cartService.Object, _settingService.Object, _notificationService.Object);

            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1, Price = 50 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            vm.TableNumber = 0;

            await vm.SaveOrderCommand.ExecuteAsync(null);

            _notificationService.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("table"))), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }

        [Fact]
        public async Task SaveOrderAsync_DialogConfirmed_SavesOrderSuccessfully()
        {
            var mockDialogService = new Mock<IDialogService>();
            mockDialogService
                .Setup(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()))
                .ReturnsAsync(true);

            BillingViewModel.RegisterTestServices(
                _itemService.Object, _orderService.Object, _categoryService.Object,
                _cashService.Object, _tableService.Object);
            var localVm = new BillingViewModel(
                _cartService.Object, _settingService.Object, _notificationService.Object,
                mockDialogService.Object);

            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 100 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

            await localVm.SaveOrderCommand.ExecuteAsync(null);

            mockDialogService.Verify(s => s.ShowConfirmCheckoutAsync(It.Is<ConfirmCheckoutDetails>(d => d.TotalItems == 2 && d.PaymentMode == PaymentModes.Cash)), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_DialogCancelled_AbortsCheckout()
        {
            var mockDialogService = new Mock<IDialogService>();
            mockDialogService
                .Setup(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()))
                .ReturnsAsync(false);

            BillingViewModel.RegisterTestServices(
                _itemService.Object, _orderService.Object, _categoryService.Object,
                _cashService.Object, _tableService.Object);
            var localVm = new BillingViewModel(
                _cartService.Object, _settingService.Object, _notificationService.Object,
                mockDialogService.Object);

            var items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2, Price = 100 } };
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(items);
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });

            await localVm.SaveOrderCommand.ExecuteAsync(null);

            mockDialogService.Verify(s => s.ShowConfirmCheckoutAsync(It.IsAny<ConfirmCheckoutDetails>()), Times.Once);
            _orderService.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }
    }
}

