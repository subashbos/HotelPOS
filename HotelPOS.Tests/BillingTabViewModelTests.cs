using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class BillingTabViewModelTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();

        private BillingViewModel CreateViewModel()
        {
            _cartService.Setup(x => x.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(x => x.GetActiveTables()).Returns(new List<int>());
            _cartService.Setup(x => x.GetSubtotal(It.IsAny<int>())).Returns(0m);
            _cartService.Setup(x => x.GetGstAmount(It.IsAny<int>())).Returns(0m);
            _cartService.Setup(x => x.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());

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
        public void SelectTable_ChangesTableNumber_WhenNotInTransferMode()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.TableNumber = 1;

            // Act
            vm.SelectTableCommand.Execute(5);

            // Assert
            Assert.Equal(5, vm.TableNumber);
            Assert.False(vm.IsTransferMode);
        }

        [Fact]
        public void SelectTable_PerformsTransfer_WhenInTransferMode()
        {
            // Arrange
            var vm = CreateViewModel();
            vm.TableNumber = 1;

            // Mock items in cart to allow transfer mode
            _cartService.Setup(x => x.GetItems(1)).Returns(new List<OrderItem> { new OrderItem { ItemId = 101, ItemName = "Test" } });

            // Force Cart to populate
            vm.TableNumber = 2; // trigger change
            vm.TableNumber = 1; // trigger change back to 1

            Assert.NotEmpty(vm.Cart);

            vm.ToggleTransferModeCommand.Execute(null);
            Assert.True(vm.IsTransferMode);

            // Act
            vm.SelectTableCommand.Execute(5);

            // Assert
            _cartService.Verify(x => x.TransferTable(1, 5), Times.Once);
            Assert.False(vm.IsTransferMode);
            Assert.Equal(5, vm.TableNumber);
        }

        [Fact]
        public void ActiveTabs_SynchronizesWithCartService()
        {
            // Arrange
            var vm = CreateViewModel();
            _cartService.Setup(x => x.GetActiveTables()).Returns(new List<int> { 1, 3 });
            _cartService.Setup(x => x.GetItems(1)).Returns(new List<OrderItem> { new OrderItem { ItemId = 101 } });

            // Act - Trigger UpdateCart which syncs tabs
            vm.GetType().GetMethod("UpdateCart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(vm, null);

            // Assert
            Assert.Contains(1, vm.ActiveTabs);
            Assert.Contains(3, vm.ActiveTabs);
            Assert.Equal(2, vm.ActiveTabs.Count);
        }

        [Fact]
        public void ToggleTransferMode_DoesNothing_WhenCartIsEmpty()
        {
            // Arrange
            var vm = CreateViewModel();
            _cartService.Setup(x => x.GetItems(vm.TableNumber)).Returns(new List<OrderItem>());

            // Act
            vm.ToggleTransferModeCommand.Execute(null);

            // Assert
            Assert.False(vm.IsTransferMode);
        }
    }
}
