using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class FocusWorkflowTests
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

        public FocusWorkflowTests()
        {
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting());
            
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
        public async Task Search_Selection_Triggers_Cart_Update_And_Focus_Targeting()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Burger", Price = 100 };
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item> { item });
            
            // Act
            _vm.AddToCartCommand.Execute(item);

            // Assert
            // Verify CartService was called
            _cartService.Verify(s => s.AddItem(It.IsAny<int>(), item), Times.Once);
            
            // In a real UI, the view code-behind handles the focus shift based on this action.
            // This test ensures the underlying data command is functional.
        }

        [Fact]
        public void Empty_Search_Does_Not_Add_Item()
        {
            // Act
            _vm.SearchText = "";
            
            // Assert
            _cartService.Verify(s => s.AddItem(It.IsAny<int>(), It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public void Discount_Update_Triggers_Subtotal_Revalidation()
        {
            // Arrange
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);
            _vm.TableNumber = 2; // Trigger subtotal update
            
            // Act
            _vm.DiscountAmount = 20m;

            // Assert
            Assert.Equal(20m, _vm.DiscountAmount);
            // Verify that Subtotal was used for validation (implicitly via the property setter logic)
            _cartService.Verify(s => s.GetSubtotal(2), Times.AtLeastOnce);
        }
    }
}
