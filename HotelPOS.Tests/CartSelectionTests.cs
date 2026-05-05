using HotelPOS.ViewModels;
using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;
using System.Collections.Generic;
using HotelPOS.Domain;
using System.Linq;

namespace HotelPOS.Tests
{
    public class CartSelectionTests
    {
        [Fact]
        public void UpdateCart_PreservesExistingObjectReferences()
        {
            // Arrange
            var mockItemService = new Mock<IItemService>();
            var mockCartService = new Mock<ICartService>();
            var mockOrderService = new Mock<IOrderService>();
            var mockSettingService = new Mock<ISettingService>();
            var mockCatService = new Mock<ICategoryService>();
            var mockNotifyService = new Mock<INotificationService>();
            var mockCashService = new Mock<ICashService>();

            mockCartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            var vm = new BillingViewModel(
                mockItemService.Object, 
                mockCartService.Object, 
                mockOrderService.Object, 
                mockSettingService.Object,
                mockCatService.Object,
                mockNotifyService.Object,
                mockCashService.Object);

            var cartItem = new OrderItem { ItemId = 1, ItemName = "Coffee", Quantity = 1, Price = 10, Total = 10 };
            mockCartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem> { cartItem });

            // Act - Initial Load
            vm.GetType().GetMethod("UpdateCart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(vm, null);
            var firstRef = vm.Cart[0];

            // Update Quantity in mock
            cartItem.Quantity = 2;
            cartItem.Total = 20;

            // Act - Update
            vm.GetType().GetMethod("UpdateCart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(vm, null);
            var secondRef = vm.Cart[0];

            // Assert
            Assert.Same(firstRef, secondRef); // Reference must be identical to preserve selection
            Assert.Equal(2, secondRef.Quantity);
            Assert.Equal(20, secondRef.Total);
        }

        [Fact]
        public void CartRow_PropertyChange_NotifiesSubscribers()
        {
            var row = new CartRow { ItemName = "Test" };
            bool fired = false;
            row.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(CartRow.Quantity)) fired = true; };

            row.Quantity = 5;

            Assert.True(fired);
        }
    }
}
