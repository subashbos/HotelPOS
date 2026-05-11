using HotelPOS.Application;
using HotelPOS.Domain;
using Xunit;

namespace HotelPOS.Tests;

/// <summary>
/// Unit tests for CartService — the in-memory, per-table cart manager.
/// Covers all public methods, including newly added functionality.
/// </summary>
public class CartServiceTests
{
    private readonly CartService _cart = new();
    private const int Table1 = 1;
    private const int Table2 = 2;

    // ---------- Helpers ----------
    private static Item MakeItem(int id, string name, decimal price, decimal tax = 0m) =>
        new() { Id = id, Name = name, Price = price, TaxPercentage = tax };

    // ========== AddItem ===========
    [Fact]
    public void AddItem_NewItem_AddsToCartWithQuantityOne()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        var items = _cart.GetItems(Table1);
        Assert.Single(items);
        Assert.Equal("Coffee", items[0].ItemName);
        Assert.Equal(1, items[0].Quantity);
        Assert.Equal(50m, items[0].Price);
        Assert.Equal(50m, items[0].Total);
    }

    [Fact]
    public void AddItem_SameItemTwice_IncrementsQuantityAndTotal()
    {
        var item = MakeItem(1, "Coffee", 50m);
        _cart.AddItem(Table1, item);
        _cart.AddItem(Table1, item);
        var items = _cart.GetItems(Table1);
        Assert.Single(items);
        Assert.Equal(2, items[0].Quantity);
        Assert.Equal(100m, items[0].Total);
    }

    [Fact]
    public void AddItem_SameItemThreeTimes_QuantityIsThree()
    {
        var item = MakeItem(1, "Coffee", 50m);
        _cart.AddItem(Table1, item);
        _cart.AddItem(Table1, item);
        _cart.AddItem(Table1, item);
        Assert.Equal(3, _cart.GetItems(Table1)[0].Quantity);
        Assert.Equal(150m, _cart.GetItems(Table1)[0].Total);
    }

    [Fact]
    public void AddItem_TwoDifferentItems_BothAppearInCart()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(2, "Tea", 30m));
        Assert.Equal(2, _cart.GetItems(Table1).Count);
    }

    // ========== RemoveItem ===========
    [Fact]
    public void RemoveItem_ExistingItem_RemovesFromCart()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.RemoveItem(Table1, 1);
        Assert.Empty(_cart.GetItems(Table1));
    }

    [Fact]
    public void RemoveItem_OneOfMultipleItems_OnlyThatItemRemoved()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(2, "Tea", 30m));
        _cart.RemoveItem(Table1, 1);
        var items = _cart.GetItems(Table1);
        Assert.Single(items);
        Assert.Equal("Tea", items[0].ItemName);
    }

    [Fact]
    public void RemoveItem_NonExistentItemId_DoesNotThrow()
    {
        var ex = Record.Exception(() => _cart.RemoveItem(Table1, 999));
        Assert.Null(ex);
    }

    [Fact]
    public void RemoveItem_EmptyCart_DoesNotThrow()
    {
        var ex = Record.Exception(() => _cart.RemoveItem(Table1, 1));
        Assert.Null(ex);
    }

    // ========== UpdateQuantity ===========
    [Fact]
    public void UpdateQuantity_IncrementByOne_QuantityAndTotalUpdated()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.UpdateQuantity(Table1, 1, 1);
        var items = _cart.GetItems(Table1);
        Assert.Equal(2, items[0].Quantity);
        Assert.Equal(100m, items[0].Total);
    }

    [Fact]
    public void UpdateQuantity_IncrementByMany_QuantityAndTotalCorrect()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.UpdateQuantity(Table1, 1, 4);
        Assert.Equal(5, _cart.GetItems(Table1)[0].Quantity);
        Assert.Equal(250m, _cart.GetItems(Table1)[0].Total);
    }

    [Fact]
    public void UpdateQuantity_DecrementToExactlyZero_ItemRemoved()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.UpdateQuantity(Table1, 1, -1);
        Assert.Empty(_cart.GetItems(Table1));
    }

    [Fact]
    public void UpdateQuantity_DecrementBelowZero_ItemRemoved()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.UpdateQuantity(Table1, 1, -99);
        Assert.Empty(_cart.GetItems(Table1));
    }

    [Fact]
    public void UpdateQuantity_DecrementByOne_QuantityReducedByOne()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m)); // qty = 2
        _cart.UpdateQuantity(Table1, 1, -1);
        Assert.Equal(1, _cart.GetItems(Table1)[0].Quantity);
        Assert.Equal(50m, _cart.GetItems(Table1)[0].Total);
    }

    [Fact]
    public void UpdateQuantity_NonExistentItem_DoesNotThrow()
    {
        var ex = Record.Exception(() => _cart.UpdateQuantity(Table1, 999, 1));
        Assert.Null(ex);
    }

    // ========== Clear ===========
    [Fact]
    public void Clear_RemovesAllItemsForTable()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(2, "Tea", 30m));
        _cart.Clear(Table1);
        Assert.Empty(_cart.GetItems(Table1));
    }

    [Fact]
    public void Clear_EmptyTable_DoesNotThrow()
    {
        var ex = Record.Exception(() => _cart.Clear(Table1));
        Assert.Null(ex);
    }

    // ========== GetItems ===========
    [Fact]
    public void GetItems_ReturnsSortedAlphabeticallyByName()
    {
        _cart.AddItem(Table1, MakeItem(1, "Zebra Juice", 100m));
        _cart.AddItem(Table1, MakeItem(2, "Apple Juice", 80m));
        _cart.AddItem(Table1, MakeItem(3, "Mango Juice", 90m));
        var items = _cart.GetItems(Table1);
        Assert.Equal("Apple Juice", items[0].ItemName);
        Assert.Equal("Mango Juice", items[1].ItemName);
        Assert.Equal("Zebra Juice", items[2].ItemName);
    }

    [Fact]
    public void GetItems_EmptyTable_ReturnsEmptyList()
    {
        Assert.Empty(_cart.GetItems(Table1));
    }

    // ========== Totals ===========
    [Fact]
    public void GetSubtotal_EmptyCart_ReturnsZero()
    {
        Assert.Equal(0m, _cart.GetSubtotal(Table1));
    }

    [Fact]
    public void GetSubtotal_SingleItem_ReturnsItemTotal()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        Assert.Equal(50m, _cart.GetSubtotal(Table1));
    }

    [Fact]
    public void GetSubtotal_MultipleItems_SumsAllTotals()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(2, "Tea", 30m));
        Assert.Equal(80m, _cart.GetSubtotal(Table1));
    }

    [Fact]
    public void GetSubtotal_ItemAddedMultipleTimes_CorrectTotal()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m)); // qty = 2
        Assert.Equal(100m, _cart.GetSubtotal(Table1));
    }

    [Fact]
    public void GetGstAmount_FivePercent_CalculatedCorrectly()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 100m, 5m));
        Assert.Equal(5m, _cart.GetGstAmount(Table1));
    }

    [Fact]
    public void GetGstAmount_EmptyCart_ReturnsZero()
    {
        Assert.Equal(0m, _cart.GetGstAmount(Table1));
    }

    [Fact]
    public void GetGrandTotal_EqualsSubtotalPlusGst()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 100m, 5m));
        Assert.Equal(105m, _cart.GetGrandTotal(Table1));
    }

    [Fact]
    public void GetGstAmount_MixedTaxRates_CalculatedCorrectly()
    {
        // 100 @ 5% = 5
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 100m, 5m));
        // 200 @ 12% = 24
        _cart.AddItem(Table1, MakeItem(2, "Pizza", 200m, 12m));
        // Total GST = 5 + 24 = 29
        Assert.Equal(29m, _cart.GetGstAmount(Table1));
    }

    [Fact]
    public void GetGrandTotal_EmptyCart_ReturnsZero()
    {
        Assert.Equal(0m, _cart.GetGrandTotal(Table1));
    }

    // ========== Multi-table isolation ===========
    [Fact]
    public void AddItem_DifferentTables_CartsAreIsolated()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table2, MakeItem(2, "Tea", 30m));
        Assert.Single(_cart.GetItems(Table1));
        Assert.Single(_cart.GetItems(Table2));
        Assert.Equal("Coffee", _cart.GetItems(Table1)[0].ItemName);
        Assert.Equal("Tea", _cart.GetItems(Table2)[0].ItemName);
    }

    [Fact]
    public void Clear_OneTable_DoesNotAffectOtherTable()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table2, MakeItem(2, "Tea", 30m));
        _cart.Clear(Table1);
        Assert.Empty(_cart.GetItems(Table1));
        Assert.Single(_cart.GetItems(Table2));
    }

    [Fact]
    public void GetSubtotal_TwoTables_ReturnsCorrectPerTable()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.AddItem(Table2, MakeItem(2, "Tea", 30m));
        Assert.Equal(50m, _cart.GetSubtotal(Table1));
        Assert.Equal(30m, _cart.GetSubtotal(Table2));
    }

    // ========== Active tables ===========
    [Fact]
    public void GetActiveTables_NoActivity_ReturnsEmpty()
    {
        var active = _cart.GetActiveTables();
        Assert.Empty(active);
    }

    [Fact]
    public void GetActiveTables_WithCartItems_ReturnsTableNumber()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        var active = _cart.GetActiveTables();
        Assert.Single(active);
        Assert.Contains(Table1, active);
    }

    [Fact]
    public void GetActiveTables_WithHeldOrder_ReturnsTableNumber()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.HoldOrder(Table1, "Test Hold");
        var active = _cart.GetActiveTables();
        Assert.Single(active);
        Assert.Contains(Table1, active);
    }

    // ========== HoldOrder specifics ===========
    [Fact]
    public void GetHeldOrders_ReturnsHeldList()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.HoldOrder(Table1, "Lunch");
        var held = _cart.GetHeldOrders();
        Assert.Single(held);
        Assert.Equal(Table1, held[0].TableNumber);
        Assert.Single(held[0].Items);
    }

    [Fact]
    public void HoldOrder_NoItems_DoesNotCreateHold()
    {
        _cart.Clear(Table1);
        _cart.HoldOrder(Table1, "Empty Hold");
        var held = _cart.GetHeldOrders();
        Assert.Empty(held);
    }

    // ========== UpdatePrice ===========
    [Fact]
    public void UpdatePrice_ChangesPriceAndTotal()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        _cart.UpdatePrice(Table1, 1, 75m);
        var item = _cart.GetItems(Table1).First();
        Assert.Equal(75m, item.Price);
        Assert.Equal(75m, item.Total);
    }

    // ========== LoadItems ===========
    [Fact]
    public void LoadItems_ReplacesExistingCartItems()
    {
        _cart.AddItem(Table1, MakeItem(1, "Coffee", 50m));
        var newItems = new List<OrderItem>
        {
            new OrderItem { ItemId = 2, ItemName = "Tea", Quantity = 2, Price = 30m, TaxPercentage = 0m, Total = 60m },
            new OrderItem { ItemId = 3, ItemName = "Juice", Quantity = 1, Price = 80m, TaxPercentage = 0m, Total = 80m }
        };
        _cart.LoadItems(Table1, newItems);
        var items = _cart.GetItems(Table1);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.ItemId == 2 && i.Quantity == 2 && i.Total == 60m);
        Assert.Contains(items, i => i.ItemId == 3 && i.Quantity == 1 && i.Total == 80m);
    }

    // ========== Thread safety ===========
    [Fact]
    public async Task AddItem_ConcurrentAddSameItem_QuantityIsConsistent()
    {
        var item = MakeItem(1, "Coffee", 10m);
        const int threadCount = 50;
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() => _cart.AddItem(Table1, item)))
            .ToArray();
        await Task.WhenAll(tasks);
        var items = _cart.GetItems(Table1);
        Assert.Single(items);
        Assert.Equal(threadCount, items[0].Quantity);
    }

    [Fact]
    public async Task Clear_CalledConcurrentlyWithAdd_DoesNotThrow()
    {
        var item = MakeItem(1, "Coffee", 10m);
        var ex = await Record.ExceptionAsync(async () =>
        {
            var t1 = Task.Run(() => { for (int i = 0; i < 100; i++) _cart.AddItem(Table1, item); });
            var t2 = Task.Run(() => { for (int i = 0; i < 100; i++) _cart.Clear(Table1); });
            await Task.WhenAll(t1, t2);
        });
        Assert.Null(ex);
    }
}
