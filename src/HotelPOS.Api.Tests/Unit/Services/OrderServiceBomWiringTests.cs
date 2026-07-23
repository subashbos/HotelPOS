using HotelPOS.Application;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class OrderServiceBomWiringTests
    {
        private readonly Mock<IOrderRepository> _repoMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<IItemService> _itemServiceMock = new();
        private readonly Mock<IBomService> _bomServiceMock = new();

        public OrderServiceBomWiringTests()
        {
            _itemServiceMock.Setup(s => s.GetItemsByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync((List<int> ids) => ids.Select(id => new Item { Id = id, Name = "Chicken Curry", Price = 100m }).ToList());
        }

        private OrderService BuildService(IBomService? bomService)
            => new(_repoMock.Object, _mediatorMock.Object, _itemServiceMock.Object, bomService: bomService);

        [Fact]
        public async Task SaveOrderAsync_WithBomService_DeductsIngredientStockPerItem()
        {
            var service = BuildService(_bomServiceMock.Object);
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Chicken Curry", Quantity = 2, Price = 100, Total = 200 }
            };
            _repoMock.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(10);

            await service.SaveOrderAsync(new SaveOrderRequest(items, 1));

            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task SaveOrderAsync_WithoutBomService_DoesNotThrow()
        {
            var service = BuildService(null);
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Chicken Curry", Quantity = 2, Price = 100, Total = 200 }
            };
            _repoMock.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(10);

            var orderId = await service.SaveOrderAsync(new SaveOrderRequest(items, 1));

            Assert.Equal(10, orderId);
        }

        [Fact]
        public async Task SaveOrderAsync_BomServiceThrowsInsufficientStock_RollsBackTransaction()
        {
            var service = BuildService(_bomServiceMock.Object);
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Chicken Curry", Quantity = 2, Price = 100, Total = 200 }
            };
            _repoMock.Setup(r => r.GetNextInvoiceNumberAsync(It.IsAny<string>())).ReturnsAsync("INV-001");
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(10);
            _bomServiceMock.Setup(b => b.DeductIngredientStockAsync(1, 2))
                .ThrowsAsync(new InvalidOperationException("Insufficient stock for raw material: Chicken."));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveOrderAsync(new SaveOrderRequest(items, 1)));

            _repoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _repoMock.Verify(r => r.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task VoidOrderAsync_WithBomService_RestoresIngredientStock()
        {
            var service = BuildService(_bomServiceMock.Object);
            var order = new Order
            {
                Id = 5,
                Status = "Paid",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 3 } }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(5)).ReturnsAsync(order);

            await service.VoidOrderAsync(5, "Customer cancelled", "admin");

            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, -3), Times.Once);
        }

        [Fact]
        public async Task UpdateOrderAsync_WithBomService_ReturnsOldAndDeductsNewIngredientStock()
        {
            var service = BuildService(_bomServiceMock.Object);
            var oldOrder = new Order { Id = 1, Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 1 } } };
            var newOrder = new Order { Id = 1, Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 3 } } };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(1)).ReturnsAsync(oldOrder);

            await service.UpdateOrderAsync(newOrder);

            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, -1), Times.Once);
            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, 3), Times.Once);
        }

        [Fact]
        public async Task DeleteOrderAsync_WithBomService_RestoresIngredientStock()
        {
            var service = BuildService(_bomServiceMock.Object);
            var order = new Order { Id = 7, Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2 } } };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(7)).ReturnsAsync(order);

            await service.DeleteOrderAsync(7);

            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, -2), Times.Once);
        }

        [Fact]
        public async Task RefundOrderAsync_WithBomService_RestoresIngredientStockForRefundedQuantity()
        {
            var service = BuildService(_bomServiceMock.Object);
            var order = new Order
            {
                Id = 9,
                Status = "Paid",
                Items = new List<OrderItem> { new OrderItem { ItemId = 1, ItemName = "Chicken Curry", Quantity = 4, Price = 100, Total = 400 } }
            };
            _repoMock.Setup(r => r.GetByIdWithItemsAsync(9)).ReturnsAsync(order);

            await service.RefundOrderAsync(9, new List<OrderItemRefundDto> { new OrderItemRefundDto(1, 1) }, "Customer complaint");

            _bomServiceMock.Verify(b => b.DeductIngredientStockAsync(1, -1), Times.Once);
        }
    }
}
