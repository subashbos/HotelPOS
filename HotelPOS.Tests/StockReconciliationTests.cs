using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class StockReconciliationTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo = new();
    private readonly Mock<IItemService> _mockItemService = new();
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly OrderService _service;

    public StockReconciliationTests()
    {
        _service = new OrderService(_mockOrderRepo.Object, _mockMediator.Object, _mockItemService.Object);
    }

    [Fact]
    public async Task UpdateOrderAsync_ReconcilesStockCorrectly()
    {
        // Arrange
        var itemId = 1;
        var oldQty = 5;
        var newQty = 3;

        var oldOrder = new Order
        {
            Id = 123,
            Items = new List<OrderItem>
            {
                new OrderItem { ItemId = itemId, Quantity = oldQty, Price = 100, Total = 500 }
            }
        };

        var updatedOrder = new Order
        {
            Id = 123,
            Items = new List<OrderItem>
            {
                new OrderItem { ItemId = itemId, Quantity = newQty, Price = 100, Total = 300 }
            }
        };

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(123))
                     .ReturnsAsync(oldOrder);

        // Act
        await _service.UpdateOrderAsync(updatedOrder);

        // Assert
        // 1. Should return old stock (Deduct with negative quantity)
        _mockItemService.Verify(s => s.DeductStockAsync(itemId, -oldQty), Times.Once);

        // 2. Should deduct new stock
        _mockItemService.Verify(s => s.DeductStockAsync(itemId, newQty), Times.Once);

        // 3. Should update the order in repo
        _mockOrderRepo.Verify(r => r.UpdateAsync(updatedOrder), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_HandlesMultipleItems_ReconcilesStockCorrectly()
    {
        // Arrange
        var item1Id = 1;
        var item2Id = 2;

        var oldOrder = new Order
        {
            Id = 456,
            Items = new List<OrderItem>
            {
                new OrderItem { ItemId = item1Id, Quantity = 2, Price = 50, Total = 100 },
                new OrderItem { ItemId = item2Id, Quantity = 1, Price = 200, Total = 200 }
            }
        };

        var updatedOrder = new Order
        {
            Id = 456,
            Items = new List<OrderItem>
            {
                new OrderItem { ItemId = item1Id, Quantity = 1, Price = 50, Total = 50 }
                // Item 2 removed
            }
        };

        _mockOrderRepo.Setup(r => r.GetByIdWithItemsAsync(456))
                     .ReturnsAsync(oldOrder);

        // Act
        await _service.UpdateOrderAsync(updatedOrder);

        // Assert
        // Item 1: Return 2, Deduct 1
        _mockItemService.Verify(s => s.DeductStockAsync(item1Id, -2), Times.Once);
        _mockItemService.Verify(s => s.DeductStockAsync(item1Id, 1), Times.Once);

        // Item 2: Return 1, Deduct 0
        _mockItemService.Verify(s => s.DeductStockAsync(item2Id, -1), Times.Once);
        _mockItemService.Verify(s => s.DeductStockAsync(item2Id, It.IsAny<int>()), Times.Exactly(1)); // Only the return call
    }
}
