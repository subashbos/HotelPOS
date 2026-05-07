
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using System.Collections.Concurrent;

namespace HotelPOS.Application
{
    public class CartService : ICartService
    {
        // ConcurrentDictionary for safe outer table-key access across threads
        private readonly ConcurrentDictionary<int, List<OrderItem>> _tableCarts = new();
        private readonly Lock _lock = new();

        public void AddItem(int tableNumber, Item item)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var existing = items.FirstOrDefault(x => x.ItemId == item.Id);

                if (existing != null)
                {
                    existing.Quantity++;
                    existing.Total = existing.Price * existing.Quantity;
                }
                else
                {
                    items.Add(new OrderItem
                    {
                        ItemId = item.Id,
                        ItemName = item.Name,
                        Quantity = 1,
                        Price = item.Price,
                        TaxPercentage = item.TaxPercentage,
                        Total = item.Price
                    });
                }
            }
        }

        public void AddItem(int tableNumber, int itemId, int quantity)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var existing = items.FirstOrDefault(x => x.ItemId == itemId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                    if (existing.Quantity <= 0) items.Remove(existing);
                    else existing.Total = existing.Price * existing.Quantity;
                }
            }
        }

        public void RemoveItem(int tableNumber, int itemId)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var item = items.FirstOrDefault(x => x.ItemId == itemId);
                if (item != null)
                {
                    items.Remove(item);
                }
            }
        }

        public void UpdateQuantity(int tableNumber, int itemId, int change)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var item = items.FirstOrDefault(x => x.ItemId == itemId);

                if (item == null)
                {
                    return;
                }

                item.Quantity += change;

                if (item.Quantity <= 0)
                {
                    items.Remove(item);
                    return;
                }

                item.Total = item.Price * item.Quantity;
            }
        }

        public void SetQuantity(int tableNumber, int itemId, int quantity)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var item = items.FirstOrDefault(x => x.ItemId == itemId);
                if (item == null) return;

                if (quantity <= 0)
                {
                    items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                    item.Total = item.Price * item.Quantity;
                }
            }
        }

        public void Clear(int tableNumber)
        {
            lock (_lock)
            {
                GetOrCreateCart(tableNumber).Clear();
            }
        }

        public List<OrderItem> GetItems(int tableNumber)
        {
            lock (_lock)
            {
                return GetOrCreateCart(tableNumber)
                    .OrderBy(x => x.ItemName)
                    .ToList();
            }
        }

        public decimal GetSubtotal(int tableNumber)
        {
            lock (_lock)
            {
                return GetOrCreateCart(tableNumber).Sum(x => x.Total);
            }
        }

        public decimal GetGstAmount(int tableNumber)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                // Calculate tax per item. If item tax is 0, we could potentially use the default gstRate,
                // but usually 0 means tax-free. However, for compatibility with existing tests, 
                // if we want to support the parameter, we'd need logic here.
                // Let's assume item.TaxPercentage is the source of truth now.
                return Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);
            }
        }

        public decimal GetGrandTotal(int tableNumber)
        {
            var subtotal = GetSubtotal(tableNumber);
            return subtotal + GetGstAmount(tableNumber);
        }

        private List<OrderItem> GetOrCreateCart(int tableNumber)
        {
            return _tableCarts.GetOrAdd(tableNumber, _ => new List<OrderItem>());
        }

        public void LoadItems(int tableNumber, List<OrderItem> items)
        {
            lock (_lock)
            {
                var cart = GetOrCreateCart(tableNumber);
                cart.Clear();
                foreach (var item in items)
                {
                    cart.Add(new OrderItem
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TaxPercentage = item.TaxPercentage,
                        Total = item.Total
                    });
                }
            }
        }

        public void UpdatePrice(int tableNumber, int itemId, decimal newPrice)
        {
            lock (_lock)
            {
                var cart = GetOrCreateCart(tableNumber);
                var item = cart.FirstOrDefault(x => x.ItemId == itemId);
                if (item != null)
                {
                    item.Price = newPrice;
                    item.Total = item.Price * item.Quantity;
                }
            }
        }

        private readonly ConcurrentList<HeldOrder> _heldOrders = new();

        public void HoldOrder(int tableNumber, string holdName)
        {
            lock (_lock)
            {
                var items = GetItems(tableNumber);
                if (items.Count == 0) return;

                var held = new HeldOrder
                {
                    HoldName = string.IsNullOrWhiteSpace(holdName) ? $"Table {tableNumber}" : holdName,
                    HeldAt = DateTime.Now,
                    TableNumber = tableNumber,
                    Items = items.Select(x => new OrderItem
                    {
                        ItemId = x.ItemId,
                        ItemName = x.ItemName,
                        Quantity = x.Quantity,
                        Price = x.Price,
                        TaxPercentage = x.TaxPercentage,
                        Total = x.Total
                    }).ToList()
                };

                _heldOrders.Add(held);
                Clear(tableNumber);
            }
        }

        public List<HeldOrder> GetHeldOrders() => _heldOrders.ToList();

        public void ResumeHeldOrder(Guid heldOrderId, int targetTableNumber)
        {
            lock (_lock)
            {
                var held = _heldOrders.FirstOrDefault(x => x.Id == heldOrderId);
                if (held == null) return;

                LoadItems(targetTableNumber, held.Items);
                _heldOrders.Remove(held);
            }
        }

        public void TransferTable(int sourceTableNumber, int targetTableNumber)
        {
            if (sourceTableNumber == targetTableNumber) return;

            lock (_lock)
            {
                var sourceItems = GetItems(sourceTableNumber);
                if (sourceItems.Count == 0) return;

                var targetItems = GetOrCreateCart(targetTableNumber);

                foreach (var item in sourceItems)
                {
                    var existing = targetItems.FirstOrDefault(x => x.ItemId == item.ItemId);
                    if (existing != null)
                    {
                        existing.Quantity += item.Quantity;
                        existing.Total = existing.Price * existing.Quantity;
                    }
                    else
                    {
                        targetItems.Add(new OrderItem
                        {
                            ItemId = item.ItemId,
                            ItemName = item.ItemName,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            TaxPercentage = item.TaxPercentage,
                            Total = item.Total
                        });
                    }
                }

                Clear(sourceTableNumber);
            }
        }

        public List<int> GetActiveTables()
        {
            lock (_lock)
            {
                var active = new List<int>();
                foreach (var tableNumber in _tableCarts.Keys)
                {
                    if (_tableCarts.TryGetValue(tableNumber, out var items) && items.Count > 0)
                    {
                        active.Add(tableNumber);
                    }
                }

                var held = _heldOrders.ToList().Select(h => h.TableNumber);

                return active.Union(held).Distinct().OrderBy(x => x).ToList();
            }
        }
    }

    // Small helper for thread-safe list if needed, or just use lock
    public class ConcurrentList<T>
    {
        private readonly List<T> _list = new();
        private readonly object _sync = new();
        public void Add(T item) { lock (_sync) _list.Add(item); }
        public bool Remove(T item) { lock (_sync) return _list.Remove(item); }
        public List<T> ToList() { lock (_sync) return new List<T>(_list); }
        public T? FirstOrDefault(Func<T, bool> predicate) { lock (_sync) return _list.FirstOrDefault(predicate); }
    }
}
