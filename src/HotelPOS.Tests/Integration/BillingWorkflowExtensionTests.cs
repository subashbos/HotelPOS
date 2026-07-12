using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingWorkflowExtensionTests
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

        public BillingWorkflowExtensionTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { ReceiptFormat = "Thermal" });
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession { Id = 1 });
            
            BillingViewModel.RegisterTestServices(
                _itemService.Object, _orderService.Object, _categoryService.Object,
                _cashService.Object, _tableService.Object);
            _vm = new BillingViewModel(
                _cartService.Object,
                _settingService.Object,
                _notificationService.Object);
        }

        [Fact]
        public void CancelEdit_ClearsCart_And_FiresEvent()
        {
            // Arrange
            bool eventFired = false;
            _vm.OrderEditCancelled += () => eventFired = true;
            _vm.LoadOrderForEdit(new Order { TableNumber = 5 }); // Initialize edit mode

            // Act
            _vm.CancelEditCommand.Execute(null);

            // Assert
            _cartService.Verify(s => s.Clear(5), Times.Once);
            Assert.False(_vm.IsEditMode);
            Assert.True(eventFired);
            Assert.Equal(OrderTypes.DineIn, _vm.OrderType);
        }

        [Fact]
        public void ToggleOrderType_CyclesThroughTypes()
        {
            // Arrange
            _vm.OrderType = OrderTypes.DineIn;

            // Act & Assert cycle
            _vm.ToggleOrderTypeCommand.Execute(null);
            Assert.Equal(OrderTypes.Takeaway, _vm.OrderType);

            _vm.ToggleOrderTypeCommand.Execute(null);
            Assert.Equal(OrderTypes.Online, _vm.OrderType);

            _vm.ToggleOrderTypeCommand.Execute(null);
            Assert.Equal(OrderTypes.DineIn, _vm.OrderType);
        }

        [Fact]
        public void LastDineInTable_PersistsDuringTakeawaySwap()
        {
            // Arrange
            _vm.TableNumber = 8;
            _vm.OrderType = OrderTypes.DineIn;

            // Act: Switch to Takeaway
            _vm.OrderType = OrderTypes.Takeaway;
            Assert.Equal(0, _vm.TableNumber); // Should be virtual table 0

            // Act: Switch back to DineIn
            _vm.OrderType = OrderTypes.DineIn;
            Assert.Equal(8, _vm.TableNumber); // Should restore previous table 8
        }

        [Fact]
        public async Task PrintKOTOnly_DoesNotClearCart()
        {
            // Arrange
            _vm.Cart.Add(new CartRow { ItemId = 1, ItemName = "Coffee", Quantity = 2 });
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { new OrderItem { ItemName = "Coffee" } });

            // Act
            await _vm.PrintKOTOnlyCommand.ExecuteAsync(null);

            // Assert
            // Should call GetItems but NOT Clear or HoldOrder
            _cartService.Verify(s => s.GetItems(It.IsAny<int>()), Times.Once);
            _cartService.Verify(s => s.Clear(It.IsAny<int>()), Times.Never);
            _cartService.Verify(s => s.HoldOrder(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            Assert.Equal("KOT Sent to Kitchen", _vm.StatusMessage);
        }
    }
}

