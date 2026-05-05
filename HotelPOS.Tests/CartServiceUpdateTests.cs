using HotelPOS.Application;
using HotelPOS.Domain;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Extended CartService tests covering update-related methods:
    /// SetQuantity, UpdatePrice (edge cases), LoadItems (edge cases).
    /// </summary>
    public class CartServiceUpdateTests
    {
        private readonly CartService _cart = new();
        private const int T1 = 1;

        private static Item MakeItem(int id, string name, decimal price, decimal tax = 0m) =>
            new() { Id = id, Name = name, Price = price, TaxPercentage = tax };

        // ========== SetQuantity ===========

        [Fact]
        public void SetQuantity_SetsAbsoluteQuantity()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m)); // qty = 2

            _cart.SetQuantity(T1, 1, 5);

            var item = _cart.GetItems(T1)[0];
            Assert.Equal(5, item.Quantity);
            Assert.Equal(250m, item.Total);
        }

        [Fact]
        public void SetQuantity_ToZero_RemovesItem()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));

            _cart.SetQuantity(T1, 1, 0);

            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void SetQuantity_ToNegative_RemovesItem()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));

            _cart.SetQuantity(T1, 1, -10);

            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void SetQuantity_NonExistentItem_DoesNotThrow()
        {
            var ex = Record.Exception(() => _cart.SetQuantity(T1, 999, 3));
            Assert.Null(ex);
        }

        [Fact]
        public void SetQuantity_UpdatesSubtotalCorrectly()
        {
            _cart.AddItem(T1, MakeItem(1, "Burger", 100m));
            _cart.SetQuantity(T1, 1, 4);

            Assert.Equal(400m, _cart.GetSubtotal(T1));
        }

        // ========== UpdatePrice edge cases ===========

        [Fact]
        public void UpdatePrice_WithMultipleQuantity_TotalIsNewPriceTimesQty()
        {
            _cart.AddItem(T1, MakeItem(1, "Pizza", 100m));
            _cart.AddItem(T1, MakeItem(1, "Pizza", 100m)); // qty = 2

            _cart.UpdatePrice(T1, 1, 120m);

            var item = _cart.GetItems(T1)[0];
            Assert.Equal(120m, item.Price);
            Assert.Equal(240m, item.Total); // 120 × 2
        }

        [Fact]
        public void UpdatePrice_NonExistentItem_DoesNotThrow()
        {
            var ex = Record.Exception(() => _cart.UpdatePrice(T1, 999, 50m));
            Assert.Null(ex);
        }

        [Fact]
        public void UpdatePrice_ToZero_SetsZeroTotal()
        {
            _cart.AddItem(T1, MakeItem(1, "Item", 50m));

            _cart.UpdatePrice(T1, 1, 0m);

            var item = _cart.GetItems(T1)[0];
            Assert.Equal(0m, item.Price);
            Assert.Equal(0m, item.Total);
        }

        [Fact]
        public void UpdatePrice_DoesNotAffectTaxPercentage()
        {
            _cart.AddItem(T1, MakeItem(1, "Taxed Item", 100m, tax: 18m));

            _cart.UpdatePrice(T1, 1, 200m);

            var item = _cart.GetItems(T1)[0];
            Assert.Equal(18m, item.TaxPercentage); // tax rate unchanged
        }

        [Fact]
        public void UpdatePrice_GstAmountReflectsNewPrice()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 100m, tax: 5m));
            _cart.UpdatePrice(T1, 1, 200m);

            // GST = 5% of 200 = 10
            Assert.Equal(10m, _cart.GetGstAmount(T1));
            Assert.Equal(210m, _cart.GetGrandTotal(T1));
        }

        // ========== LoadItems edge cases ===========

        [Fact]
        public void LoadItems_PreservesAllFieldsFromOrderItem()
        {
            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 7, ItemName = "Special Dish", Quantity = 3, Price = 250m, TaxPercentage = 12m, Total = 750m }
            };

            _cart.LoadItems(T1, items);

            var loaded = _cart.GetItems(T1)[0];
            Assert.Equal(7, loaded.ItemId);
            Assert.Equal("Special Dish", loaded.ItemName);
            Assert.Equal(3, loaded.Quantity);
            Assert.Equal(250m, loaded.Price);
            Assert.Equal(12m, loaded.TaxPercentage);
            Assert.Equal(750m, loaded.Total);
        }

        [Fact]
        public void LoadItems_EmptyList_ClearsCart()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));

            _cart.LoadItems(T1, new List<OrderItem>());

            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void LoadItems_DoesNotShareReferenceWithSource()
        {
            var source = new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Tea", Quantity = 1, Price = 30m, Total = 30m }
            };

            _cart.LoadItems(T1, source);

            // Mutate source — cart should be unaffected
            source[0].ItemName = "MUTATED";

            Assert.Equal("Tea", _cart.GetItems(T1)[0].ItemName);
        }

        [Fact]
        public void LoadItems_SubtotalReflectsLoadedItems()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 500m)); // high value first

            var items = new List<OrderItem>
            {
                new OrderItem { ItemId = 2, ItemName = "Tea", Quantity = 2, Price = 30m, Total = 60m },
                new OrderItem { ItemId = 3, ItemName = "Snack", Quantity = 1, Price = 40m, Total = 40m }
            };
            _cart.LoadItems(T1, items);

            Assert.Equal(100m, _cart.GetSubtotal(T1)); // 60 + 40, not 500+
        }

        // ========== UpdateQuantity used as edit scenario ===========

        [Fact]
        public void UpdateQuantity_Sequence_SimulatesEditFlow()
        {
            // Load an existing order into the cart
            _cart.LoadItems(T1, new List<OrderItem>
            {
                new OrderItem { ItemId = 1, ItemName = "Biryani", Quantity = 2, Price = 150m, TaxPercentage = 5m, Total = 300m },
                new OrderItem { ItemId = 2, ItemName = "Lassi", Quantity = 1, Price = 50m, TaxPercentage = 0m, Total = 50m }
            });

            // User increments Biryani qty
            _cart.UpdateQuantity(T1, 1, 1); // qty = 3

            // User removes Lassi entirely
            _cart.RemoveItem(T1, 2);

            var items = _cart.GetItems(T1);
            Assert.Single(items);
            Assert.Equal("Biryani", items[0].ItemName);
            Assert.Equal(3, items[0].Quantity);
            Assert.Equal(450m, items[0].Total);
            Assert.Equal(450m, _cart.GetSubtotal(T1));
        }
    }
}
