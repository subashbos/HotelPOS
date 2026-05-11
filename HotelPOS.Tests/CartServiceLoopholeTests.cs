using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers CartService edge cases missing from CartServiceTests.cs:
    /// SetQuantity to 0/negative, UpdatePrice not-found, TransferTable same/empty,
    /// ResumeHeldOrder invalid guid, and hold/resume round-trip.
    /// </summary>
    public class CartServiceLoopholeTests
    {
        private readonly CartService _cart = new();
        private const int T1 = 1;
        private const int T2 = 2;
        private const int T3 = 3;

        private static Item MakeItem(int id, string name, decimal price, decimal tax = 0m) =>
            new() { Id = id, Name = name, Price = price, TaxPercentage = tax };

        // ── SetQuantity ──────────────────────────────────────────────────────

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
            _cart.SetQuantity(T1, 1, -5);
            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void SetQuantity_ToPositive_UpdatesQuantityAndTotal()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.SetQuantity(T1, 1, 4);
            var item = _cart.GetItems(T1)[0];
            Assert.Equal(4, item.Quantity);
            Assert.Equal(200m, item.Total);
        }

        [Fact]
        public void SetQuantity_NonExistentItem_DoesNotThrow()
        {
            var ex = Record.Exception(() => _cart.SetQuantity(T1, 999, 3));
            Assert.Null(ex);
        }

        // ── UpdatePrice ──────────────────────────────────────────────────────

        [Fact]
        public void UpdatePrice_NonExistentItem_DoesNotThrow()
        {
            var ex = Record.Exception(() => _cart.UpdatePrice(T1, 999, 100m));
            Assert.Null(ex);
        }

        [Fact]
        public void UpdatePrice_ExistingItem_UpdatesPriceAndRecalculatesTotal()
        {
            _cart.AddItem(T1, MakeItem(1, "Tea", 30m));
            _cart.AddItem(T1, MakeItem(1, "Tea", 30m)); // qty = 2
            _cart.UpdatePrice(T1, 1, 50m);
            var item = _cart.GetItems(T1)[0];
            Assert.Equal(50m, item.Price);
            Assert.Equal(100m, item.Total); // 50 * 2
        }

        // ── TransferTable ────────────────────────────────────────────────────

        [Fact]
        public void TransferTable_SameTable_DoesNothing()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.TransferTable(T1, T1);
            Assert.Single(_cart.GetItems(T1));
        }

        [Fact]
        public void TransferTable_EmptySource_DoesNothing()
        {
            _cart.AddItem(T2, MakeItem(2, "Tea", 30m));
            _cart.TransferTable(T1, T2); // T1 is empty
            // T2 should be unchanged
            Assert.Single(_cart.GetItems(T2));
            Assert.Equal("Tea", _cart.GetItems(T2)[0].ItemName);
        }

        [Fact]
        public void TransferTable_MovesItemsAndClearsSource()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m)); // qty = 2
            _cart.TransferTable(T1, T2);
            Assert.Empty(_cart.GetItems(T1));
            Assert.Single(_cart.GetItems(T2));
            Assert.Equal(2, _cart.GetItems(T2)[0].Quantity);
        }

        [Fact]
        public void TransferTable_MergesWithExistingItemsOnTarget()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m)); // T1: Coffee qty=1
            _cart.AddItem(T2, MakeItem(1, "Coffee", 50m)); // T2: Coffee qty=1
            _cart.TransferTable(T1, T2);
            // T2 should now have Coffee qty=2
            Assert.Equal(2, _cart.GetItems(T2)[0].Quantity);
            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void TransferTable_DifferentItems_BothAppearOnTarget()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.AddItem(T2, MakeItem(2, "Tea", 30m));
            _cart.TransferTable(T1, T2);
            Assert.Equal(2, _cart.GetItems(T2).Count);
            Assert.Empty(_cart.GetItems(T1));
        }

        // ── HoldOrder / ResumeHeldOrder round-trip ───────────────────────────

        [Fact]
        public void HoldAndResume_RoundTrip_RestoresItemsToTargetTable()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m)); // qty = 2
            _cart.AddItem(T1, MakeItem(2, "Tea", 30m));

            _cart.HoldOrder(T1, "Lunch Hold");

            // T1 should be empty after hold
            Assert.Empty(_cart.GetItems(T1));

            var held = _cart.GetHeldOrders();
            Assert.Single(held);

            // Resume onto T3
            _cart.ResumeHeldOrder(held[0].Id, T3);

            var restored = _cart.GetItems(T3);
            Assert.Equal(2, restored.Count);
            Assert.Contains(restored, i => i.ItemId == 1 && i.Quantity == 2);
            Assert.Contains(restored, i => i.ItemId == 2 && i.Quantity == 1);

            // Held orders list should be empty after resume
            Assert.Empty(_cart.GetHeldOrders());
        }

        [Fact]
        public void ResumeHeldOrder_InvalidGuid_DoesNotThrow()
        {
            var ex = Record.Exception(() => _cart.ResumeHeldOrder(Guid.NewGuid(), T1));
            Assert.Null(ex);
        }

        [Fact]
        public void ResumeHeldOrder_InvalidGuid_CartUnchanged()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.ResumeHeldOrder(Guid.NewGuid(), T1);
            Assert.Single(_cart.GetItems(T1));
        }

        [Fact]
        public void HoldOrder_ClearsCartAfterHolding()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.HoldOrder(T1, "Hold");
            Assert.Empty(_cart.GetItems(T1));
        }

        [Fact]
        public void HoldOrder_PreservesItemDetails()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m, 5m));
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m, 5m)); // qty = 2
            _cart.HoldOrder(T1, "Test");

            var held = _cart.GetHeldOrders()[0];
            Assert.Single(held.Items);
            Assert.Equal(2, held.Items[0].Quantity);
            Assert.Equal(50m, held.Items[0].Price);
            Assert.Equal(5m, held.Items[0].TaxPercentage);
            Assert.Equal(100m, held.Items[0].Total);
        }

        // ── GetActiveTables after clear ──────────────────────────────────────

        [Fact]
        public void GetActiveTables_AfterClear_TableRemovedFromActive()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.Clear(T1);
            Assert.DoesNotContain(T1, _cart.GetActiveTables());
        }

        [Fact]
        public void GetActiveTables_HeldOrderTable_StillActive()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 50m));
            _cart.HoldOrder(T1, "Hold");
            // Cart is cleared but held order keeps T1 active
            Assert.Contains(T1, _cart.GetActiveTables());
        }

        // ── GetGstAmount with quantity > 1 ───────────────────────────────────

        [Fact]
        public void GetGstAmount_MultipleQuantity_CalculatesOnFullTotal()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 100m, 10m));
            _cart.AddItem(T1, MakeItem(1, "Coffee", 100m, 10m)); // qty = 2
            // GST = 100 * 2 * 10% = 20
            Assert.Equal(20m, _cart.GetGstAmount(T1));
        }

        [Fact]
        public void GetGrandTotal_MultipleItemsWithTax_CorrectTotal()
        {
            _cart.AddItem(T1, MakeItem(1, "Coffee", 100m, 5m));   // 100 + 5 = 105
            _cart.AddItem(T1, MakeItem(2, "Pizza", 200m, 12m));   // 200 + 24 = 224
            Assert.Equal(329m, _cart.GetGrandTotal(T1));
        }
    }
}
