using HotelPOS.Domain;
using Xunit;

namespace HotelPOS.Tests;

/// <summary>
/// Sanity tests for Domain entity default values and initializers.
/// These guard against regression if nullable defaults are accidentally removed.
/// </summary>
public class DomainEntityTests
{
    // ========== Item ==========

    [Fact]
    public void Item_DefaultName_IsEmptyString()
    {
        var item = new Item();
        Assert.Equal(string.Empty, item.Name);
    }

    [Fact]
    public void Item_DefaultPrice_IsZero()
    {
        var item = new Item();
        Assert.Equal(0m, item.Price);
    }

    [Fact]
    public void Item_DefaultId_IsZero()
    {
        var item = new Item();
        Assert.Equal(0, item.Id);
    }

    [Fact]
    public void Item_SetName_RetainsValue()
    {
        var item = new Item { Name = "Biryani" };
        Assert.Equal("Biryani", item.Name);
    }

    [Fact]
    public void Item_SetPrice_RetainsValue()
    {
        var item = new Item { Price = 199.99m };
        Assert.Equal(199.99m, item.Price);
    }

    // ========== OrderItem ==========

    [Fact]
    public void OrderItem_DefaultItemName_IsEmptyString()
    {
        var oi = new OrderItem();
        Assert.Equal(string.Empty, oi.ItemName);
    }

    [Fact]
    public void OrderItem_DefaultOrder_IsNull()
    {
        var oi = new OrderItem();
        Assert.Null(oi.Order);
    }

    [Fact]
    public void OrderItem_DefaultQuantity_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0, oi.Quantity);
    }

    [Fact]
    public void OrderItem_DefaultPrice_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0m, oi.Price);
    }

    [Fact]
    public void OrderItem_DefaultTotal_IsZero()
    {
        var oi = new OrderItem();
        Assert.Equal(0m, oi.Total);
    }

    [Fact]
    public void OrderItem_SetValues_RetainsAllValues()
    {
        var oi = new OrderItem
        {
            Id = 5,
            OrderId = 10,
            ItemId = 3,
            ItemName = "Coffee",
            Quantity = 2,
            Price = 50m,
            Total = 100m
        };

        Assert.Equal(5, oi.Id);
        Assert.Equal(10, oi.OrderId);
        Assert.Equal(3, oi.ItemId);
        Assert.Equal("Coffee", oi.ItemName);
        Assert.Equal(2, oi.Quantity);
        Assert.Equal(50m, oi.Price);
        Assert.Equal(100m, oi.Total);
    }

    // ========== Order ==========

    [Fact]
    public void Order_DefaultItems_IsNotNull()
    {
        var order = new Order();
        Assert.NotNull(order.Items);
    }

    [Fact]
    public void Order_DefaultItems_IsEmptyList()
    {
        var order = new Order();
        Assert.Empty(order.Items);
    }

    [Fact]
    public void Order_DefaultTotalAmount_IsZero()
    {
        var order = new Order();
        Assert.Equal(0m, order.TotalAmount);
    }

    [Fact]
    public void Order_DefaultTableNumber_IsZero()
    {
        var order = new Order();
        Assert.Equal(0, order.TableNumber);
    }

    [Fact]
    public void Order_ItemsCollection_CanAddItems()
    {
        var order = new Order();
        order.Items.Add(new OrderItem { ItemName = "Tea", Price = 30m, Total = 30m });

        Assert.Single(order.Items);
        Assert.Equal("Tea", order.Items[0].ItemName);
    }

    [Fact]
    public void Order_SetValues_RetainsAllValues()
    {
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = 1,
            CreatedAt = now,
            TableNumber = 3,
            TotalAmount = 250m
        };

        Assert.Equal(1, order.Id);
        Assert.Equal(now, order.CreatedAt);
        Assert.Equal(3, order.TableNumber);
        Assert.Equal(250m, order.TotalAmount);
    }
}
