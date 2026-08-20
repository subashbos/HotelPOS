using System.Collections.Concurrent;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.State;
using HotelPOS.Domain.Entities;
using Xunit;

namespace HotelPOS.Tests
{
    public class CartStateStoreTests
    {
        [Fact]
        public void NewStore_HasEmptyTableCartsAndHeldOrders()
        {
            var store = new CartStateStore();

            Assert.Empty(store.TableCarts);
            Assert.Empty(store.HeldOrders);
            Assert.NotNull(store.LockObj);
        }

        [Fact]
        public void TableCarts_IsConcurrentDictionary()
        {
            var store = new CartStateStore();

            Assert.IsType<ConcurrentDictionary<int, List<OrderItem>>>(store.TableCarts);
        }

        [Fact]
        public void TwoInstances_DoNotShareState()
        {
            var store1 = new CartStateStore();
            var store2 = new CartStateStore();

            store1.TableCarts.GetOrAdd(5, _ => new List<OrderItem> { new OrderItem { ItemId = 1 } });

            Assert.NotEmpty(store1.TableCarts);
            Assert.Empty(store2.TableCarts);
            Assert.NotSame(store1.LockObj, store2.LockObj);
        }

        [Fact]
        public void GetOrAdd_NewTableId_CreatesFreshCart()
        {
            var store = new CartStateStore();

            var cart = store.TableCarts.GetOrAdd(1, _ => new List<OrderItem>());

            Assert.NotNull(cart);
            Assert.Empty(cart);
            Assert.True(store.TableCarts.ContainsKey(1));
        }

        [Fact]
        public void GetOrAdd_SameTableIdTwice_ReturnsSameInstance()
        {
            var store = new CartStateStore();

            var first = store.TableCarts.GetOrAdd(2, _ => new List<OrderItem>());
            first.Add(new OrderItem { ItemId = 42, Quantity = 3 });
            var second = store.TableCarts.GetOrAdd(2, _ => new List<OrderItem>());

            Assert.Same(first, second);
            Assert.Single(second);
            Assert.Equal(42, second[0].ItemId);
        }

        [Fact]
        public void TryRemove_ClearsTableState()
        {
            var store = new CartStateStore();
            store.TableCarts.GetOrAdd(3, _ => new List<OrderItem> { new OrderItem { ItemId = 1 } });

            var removed = store.TableCarts.TryRemove(3, out _);

            Assert.True(removed);
            Assert.False(store.TableCarts.ContainsKey(3));
            var recreated = store.TableCarts.GetOrAdd(3, _ => new List<OrderItem>());
            Assert.Empty(recreated);
        }

        [Fact]
        public void TableCarts_ConcurrentGetOrAdd_SameKey_AllCallersGetSameInstance()
        {
            var store = new CartStateStore();
            var results = new ConcurrentBag<List<OrderItem>>();

            Parallel.For(0, 100, _ =>
            {
                var cart = store.TableCarts.GetOrAdd(7, _ => new List<OrderItem>());
                results.Add(cart);
            });

            Assert.Single(store.TableCarts.Keys, k => k == 7);
            Assert.True(results.All(r => ReferenceEquals(r, results.First())));
        }

        [Fact]
        public void HeldOrders_AddAndRemove_SequentialBehavior()
        {
            var store = new CartStateStore();
            var held = new HeldOrder { HoldName = "Hold 1", TableNumber = 4 };

            store.HeldOrders.Add(held);
            Assert.Single(store.HeldOrders);

            store.HeldOrders.Remove(held);
            Assert.Empty(store.HeldOrders);
        }
    }
}
