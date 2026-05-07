using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Events;
using HotelPOS.Domain.Interface;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _repoMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _repoMock = new Mock<IOrderRepository>();
            _mediatorMock = new Mock<IMediator>();
            _itemServiceMock = new Mock<IItemService>();
            _service = new OrderService(_repoMock.Object, _mediatorMock.Object, _itemServiceMock.Object);
        }

        [Fact]
        public async Task SaveOrderAsync_ShouldSaveAndDeductStock()
        {
            // Arrange
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Test", Quantity = 2, Price = 100, TaxPercentage = 5, Total = 200 }
            };
            _repoMock.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(10);

            // Act
            var orderId = await _service.SaveOrderAsync(items, 1);

            // Assert
            Assert.Equal(10, orderId);
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, 2), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.Is<Order>(o => o.InvoiceNumber == "INV-001" && o.TotalAmount == 210)), Times.Once);
            _mediatorMock.Verify(m => m.Publish(It.IsAny<EntityActionEvent>(), default), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_EmptyItems_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveOrderAsync(new List<OrderItem>(), 1));
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldReconcileStock()
        {
            // Arrange
            var oldOrder = new Order
            {
                Id = 1,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 5 } }
            };
            var newOrder = new Order
            {
                Id = 1,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 3 }, new OrderItem { ItemId = 2, Quantity = 2 } }
            };

            _repoMock.Setup(r => r.GetByIdWithItemsAsync(1)).ReturnsAsync(oldOrder);

            // Act
            await _service.UpdateOrderAsync(newOrder);

            // Assert
            // Old stock returned: Deduct(1, -5)
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, -5), Times.Once);
            // New stock deducted: Deduct(1, 3) and Deduct(2, 2)
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, 3), Times.Once);
            _itemServiceMock.Verify(s => s.DeductStockAsync(2, 2), Times.Once);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task DeleteOrderAsync_ShouldReturnStockAndDelete()
        {
            // Arrange
            var existing = new Order
            {
                Id = 1,
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 10 } }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(1)).ReturnsAsync(existing);

            // Act
            await _service.DeleteOrderAsync(1);

            // Assert
            _itemServiceMock.Verify(s => s.DeductStockAsync(1, -10), Times.Once);
            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
