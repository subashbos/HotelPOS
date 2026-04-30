using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using MediatR;
using HotelPOS.Domain.Events;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

/// <summary>
/// Unit tests for OrderService — validates null/empty guards, correct
/// Order construction (UTC timestamp, totals, item cloning, table number)
/// and correct delegation to IOrderRepository via a mocked dependency.
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockRepo = new();
    private readonly Mock<IMediator> _mockMediator = new();
    private readonly Mock<IItemService> _mockItemService = new();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_mockRepo.Object, _mockMediator.Object, _mockItemService.Object);
    }
    
    // Add using MediatR and Events

    // ---------- Helpers ----------

    private static List<OrderItem> MakeSingleItem(
        int id = 1, string name = "Coffee",
        decimal price = 50m, int qty = 2) =>
        new()
        {
            new OrderItem
            {
                ItemId   = id,
                ItemName = name,
                Quantity = qty,
                Price    = price,
                TaxPercentage = 5m,
                Total    = price * qty
            }
        };

    private static List<OrderItem> MakeMultipleItems() =>
        new()
        {
            new OrderItem { ItemId = 1, ItemName = "Coffee",  Quantity = 2, Price = 50m,  TaxPercentage = 5m, Total = 100m },
            new OrderItem { ItemId = 2, ItemName = "Biryani", Quantity = 1, Price = 150m, TaxPercentage = 5m, Total = 150m },
            new OrderItem { ItemId = 3, ItemName = "Tea",     Quantity = 3, Price = 30m,  TaxPercentage = 5m, Total = 90m  },
        };

    // ========== Guard clauses ==========

    [Fact]
    public async Task SaveOrderAsync_NullItems_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveOrderAsync(null!, 1));
    }

    [Fact]
    public async Task SaveOrderAsync_EmptyItemsList_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SaveOrderAsync(new List<OrderItem>(), 1));
    }

    // ========== Repository interaction ==========

    [Fact]
    public async Task SaveOrderAsync_ValidItems_CallsRepositoryExactlyOnce()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeSingleItem(), 1);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task SaveOrderAsync_ValidItems_ReturnsIdFromRepository()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(99);

        var result = await _service.SaveOrderAsync(MakeSingleItem(), 1);

        Assert.Equal(99, result);
    }

    // ========== Order construction ==========

    [Fact]
    public async Task SaveOrderAsync_SetsCorrectTableNumber()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeSingleItem(), tableNumber: 5);

        Assert.Equal(5, captured?.TableNumber);
    }

    [Fact]
    public async Task SaveOrderAsync_CreatedAtIsUtc()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        var before = DateTime.UtcNow;
        await _service.SaveOrderAsync(MakeSingleItem(), 1);
        var after = DateTime.UtcNow;

        Assert.NotNull(captured);
        Assert.Equal(DateTimeKind.Utc, captured!.CreatedAt.Kind);
        Assert.InRange(captured.CreatedAt, before, after);
    }

    [Fact]
    public async Task SaveOrderAsync_SingleItem_TotalAmountCorrect()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        // qty=2, price=50 => Subtotal=100, GST=5 => Total=105
        await _service.SaveOrderAsync(MakeSingleItem(price: 50m, qty: 2), 1);

        Assert.Equal(105m, captured?.TotalAmount);
    }

    [Fact]
    public async Task SaveOrderAsync_MultipleItems_TotalAmountIsSumOfLineTotals()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        // Coffee 2×50=100 + Biryani 1×150=150 + Tea 3×30=90 → Subtotal 340, GST 17 → Total 357
        await _service.SaveOrderAsync(MakeMultipleItems(), 1);

        Assert.Equal(357m, captured?.TotalAmount);
    }

    [Fact]
    public async Task SaveOrderAsync_OrderContainsCorrectNumberOfItems()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeMultipleItems(), 1);

        Assert.Equal(3, captured?.Items.Count);
    }

    [Fact]
    public async Task SaveOrderAsync_CalculatesCgstAndSgstCorrectly_AndSavesCustomerDetails()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        // qty=2, price=50 => Subtotal=100, GST=5
        // Expect: CGST = 2.50, SGST = 2.50
        await _service.SaveOrderAsync(MakeSingleItem(price: 50m, qty: 2), 1, 0, "Cash", "ACME Corp", "9876543210", "27AAAAA1234A1Z5");

        Assert.NotNull(captured);
        Assert.Equal(5m, captured.GstAmount);
        Assert.Equal(2.5m, captured.CgstAmount);
        Assert.Equal(2.5m, captured.SgstAmount);
        Assert.Equal(0m, captured.IgstAmount);

        Assert.Equal("ACME Corp", captured.CustomerName);
        Assert.Equal("9876543210", captured.CustomerPhone);
        Assert.Equal("27AAAAA1234A1Z5", captured.CustomerGstin);
    }

    // ========== Item cloning (new references, same data) ==========

    [Fact]
    public async Task SaveOrderAsync_ClonesItemReferences_NotSameObjects()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        var original = MakeSingleItem();
        await _service.SaveOrderAsync(original, 1);

        // Must be a DIFFERENT object (defensive copy), not the same reference
        Assert.NotSame(original[0], captured!.Items[0]);
    }

    [Fact]
    public async Task SaveOrderAsync_ClonesItemData_AllFieldsCopiedCorrectly()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        var items = new List<OrderItem>
        {
            new() { ItemId = 7, ItemName = "Dosa", Quantity = 3, Price = 60m, Total = 180m }
        };

        await _service.SaveOrderAsync(items, 1);

        var saved = captured!.Items[0];
        Assert.Equal(7, saved.ItemId);
        Assert.Equal("Dosa", saved.ItemName);
        Assert.Equal(3, saved.Quantity);
        Assert.Equal(60m, saved.Price);
        Assert.Equal(180m, saved.Total);
    }

    [Fact]
    public async Task SaveOrderAsync_MutatingOriginalItemsAfterSave_DoesNotAffectSavedOrder()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        var original = MakeSingleItem(price: 50m, qty: 2);
        await _service.SaveOrderAsync(original, 1);

        // Mutate original — should NOT affect the saved order
        original[0].Quantity = 999;
        original[0].Total = 49999m;

        Assert.Equal(2, captured!.Items[0].Quantity);
        Assert.Equal(100m, captured.Items[0].Total);
    }

    // ========== Edge cases ==========

    [Fact]
    public async Task SaveOrderAsync_TableNumberZero_SavesWithTableZero()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeSingleItem(), tableNumber: 0);

        Assert.Equal(0, captured?.TableNumber);
    }

    [Fact]
    public async Task SaveOrderAsync_VeryHighTableNumber_SavesCorrectly()
    {
        Order? captured = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
                 .Callback<Order>(o => captured = o)
                 .ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeSingleItem(), tableNumber: 9999);

        Assert.Equal(9999, captured?.TableNumber);
    }

    [Fact]
    public async Task DeleteOrderAsync_CallsRepository()
    {
        var order = new Order { Id = 123, Items = new List<OrderItem>() };
        _mockRepo.Setup(r => r.GetByIdWithItemsAsync(123)).ReturnsAsync(order);

        await _service.DeleteOrderAsync(123);
        _mockRepo.Verify(r => r.DeleteAsync(123), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_ValidOrder_RecalculatesTotalsAndCallsRepository()
    {
        var items = new List<OrderItem>
        {
            new() { ItemName = "Item 1", Price = 100m, Quantity = 2, TaxPercentage = 5m, Total = 200m }
        };
        var order = new Order { Id = 1, Items = items };
        
        _mockRepo.Setup(r => r.GetByIdWithItemsAsync(1))
                 .ReturnsAsync(order);

        await _service.UpdateOrderAsync(order);

        // Subtotal = 200, GST = 10, Total = 210
        Assert.Equal(200m, order.Subtotal);
        Assert.Equal(10m, order.GstAmount);
        Assert.Equal(210m, order.TotalAmount);
        _mockRepo.Verify(r => r.UpdateAsync(order), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_EmptyOrder_ThrowsArgumentException()
    {
        var order = new Order { Id = 1, Items = new List<OrderItem>() };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateOrderAsync(order));
    }

    // ========== Auditing ==========

    [Fact]
    public async Task SaveOrderAsync_LogsAuditAction()
    {
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ReturnsAsync(1);

        await _service.SaveOrderAsync(MakeSingleItem(), 5);

        _mockMediator.Verify(m => m.Publish(
            It.Is<EntityActionEvent>(e => e.EntityName == "Order" && e.Action == "Create" && e.Details!.Contains("Table: 5")), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrderAsync_LogsAuditAction()
    {
        var items = MakeSingleItem();
        var order = new Order { Id = 10, Items = items };
        
        // Setup repo to return the existing order
        _mockRepo.Setup(r => r.GetByIdWithItemsAsync(10))
                 .ReturnsAsync(order);

        await _service.UpdateOrderAsync(order);

        _mockMediator.Verify(m => m.Publish(
            It.Is<EntityActionEvent>(e => e.EntityName == "Order" && e.Action == "Update" && e.Details!.Contains("Total")), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task DeleteOrderAsync_LogsAuditAction()
    {
        var order = new Order { Id = 99, Items = new List<OrderItem>() };
        _mockRepo.Setup(r => r.GetByIdWithItemsAsync(99)).ReturnsAsync(order);

        await _service.DeleteOrderAsync(99);

        _mockMediator.Verify(m => m.Publish(
            It.Is<EntityActionEvent>(e => e.EntityName == "Order" && e.Action == "Delete"), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
