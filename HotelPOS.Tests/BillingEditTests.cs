using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

using MediatR;

namespace HotelPOS.Tests
{
    public class BillingEditTests
    {
        [Fact]
        public async Task OrderService_UpdateOrderAsync_RecalculatesTotals()
        {
            // Arrange
            var mockRepo = new Mock<IOrderRepository>();
            var mockMediator = new Mock<IMediator>();
            var mockItemService = new Mock<IItemService>();
            var service = new OrderService(mockRepo.Object, mockMediator.Object, mockItemService.Object);
            
            var order = new Order
            {
                Id = 1,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, ItemName = "Test", Quantity = 2, Price = 100, TaxPercentage = 5, Total = 200 }
                }
            };

            mockRepo.Setup(r => r.GetByIdWithItemsAsync(1))
                    .ReturnsAsync(order);

            // Act
            await service.UpdateOrderAsync(order);

            // Assert
            Assert.Equal(200, order.Subtotal);
            Assert.Equal(10, order.GstAmount); // 5% of 200
            Assert.Equal(210, order.TotalAmount);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public void CartService_UpdatePrice_UpdatesLineTotal()
        {
            // Arrange
            var cartService = new CartService();
            var item = new Item { Id = 1, Name = "Burger", Price = 50, TaxPercentage = 5 };
            cartService.AddItem(1, item); // Initial: 50 * 1 = 50

            // Act
            cartService.UpdatePrice(1, 1, 60);

            // Assert
            var cartItems = cartService.GetItems(1);
            Assert.Equal(60, cartItems[0].Price);
            Assert.Equal(60, cartItems[0].Total);
            Assert.Equal(63, cartService.GetGrandTotal(1)); // 60 + 5%
        }

        [Fact]
        public void CartService_LoadItems_ClearsOldAndAddsNew()
        {
            // Arrange
            var cartService = new CartService();
            cartService.AddItem(1, new Item { Id = 1, Name = "Old", Price = 10 });
            
            var newItems = new List<OrderItem>
            {
                new OrderItem { ItemId = 2, ItemName = "New", Quantity = 1, Price = 20, TaxPercentage = 0, Total = 20 }
            };

            // Act
            cartService.LoadItems(1, newItems);

            // Assert
            var items = cartService.GetItems(1);
            Assert.Single(items);
            Assert.Equal("New", items[0].ItemName);
            Assert.Equal(20, cartService.GetGrandTotal(1));
        }
    }
}
