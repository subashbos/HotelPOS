
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelPOS.Application.UseCases
{
    public class CartService : ICartService
    {
        // Per-table cart storage, guarded by _lock for thread safety
        private readonly Dictionary<int, List<OrderItem>> _tableCarts = new();
        private readonly Lock _lock = new();
        private readonly IServiceScopeFactory? _scopeFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<CartService>? _logger;

        /// <summary>
        /// Path to the cart persistence file. Overridable for testing.
        /// Pass null to disable file-based persistence entirely.
        /// </summary>
        private readonly string? _cartsFilePath;

        /// <summary>
        /// Production constructor: resolves scope factory from DI and uses the
        /// default carts.json path in the application's base directory.
        /// </summary>
        public CartService(IServiceScopeFactory? scopeFactory = null)
            : this(scopeFactory, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "carts.json"), null)
        {
        }

        internal CartService(IServiceScopeFactory? scopeFactory, string? cartsFilePath)
            : this(scopeFactory, cartsFilePath, null)
        {
        }

        internal CartService(IServiceScopeFactory? scopeFactory, string? cartsFilePath, Microsoft.Extensions.Logging.ILogger<CartService>? logger)
        {
            _scopeFactory = scopeFactory;
            _cartsFilePath = cartsFilePath;
            _logger = logger;
            RestoreCarts();
            RestoreHeldOrders();
        }

        private void LogException(Exception ex, string message)
        {
            if (_logger != null)
            {
                _logger.LogError(ex, "{Message}", message);
            }
            else
            {
                Serilog.Log.Error(ex, "{Message}", message);
            }
        }

        // ── Persistence helpers ──────────────────────────────────────────────

        private void SaveCarts()
        {
            if (_cartsFilePath == null) return;
            try
            {
                var json = JsonSerializer.Serialize(_tableCarts);
                File.WriteAllText(_cartsFilePath, json);
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to save carts");
            }
        }

        private void RestoreCarts()
        {
            if (_cartsFilePath == null) return;
            try
            {
                if (File.Exists(_cartsFilePath))
                {
                    var json = File.ReadAllText(_cartsFilePath);
                    var data = JsonSerializer.Deserialize<Dictionary<int, List<OrderItem>>>(json);
                    if (data != null)
                    {
                        _tableCarts.Clear();
                        foreach (var kvp in data)
                        {
                            _tableCarts[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to restore carts");
            }
        }

        private void SaveHeldOrderToDb(HeldOrder held)
        {
            if (_scopeFactory == null) return;
            try
            {
                Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IHeldOrderRepository>();
                    await repo.SaveAsync(held);
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to save held order to DB");
            }
        }

        private void RemoveHeldOrderFromDb(Guid id)
        {
            if (_scopeFactory == null) return;
            try
            {
                Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IHeldOrderRepository>();
                    await repo.DeleteAsync(id);
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to remove held order from DB");
            }
        }

        private void ClearAllHeldOrdersFromDb()
        {
            if (_scopeFactory == null) return;
            try
            {
                Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IHeldOrderRepository>();
                    await repo.ClearAllAsync();
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to clear held orders from DB");
            }
        }

        private void RestoreHeldOrders()
        {
            if (_scopeFactory == null) return;
            try
            {
                var restored = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IHeldOrderRepository>();
                    return await repo.GetAllAsync();
                }).GetAwaiter().GetResult();

                lock (_lock)
                {
                    _heldOrders.Clear();
                    foreach (var h in restored)
                    {
                        _heldOrders.Add(h);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex, "Failed to restore held orders from DB");
            }
        }

        // ── Cart operations ──────────────────────────────────────────────────

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
                SaveCarts();
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
                SaveCarts();
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
                SaveCarts();
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
                    SaveCarts();
                    return;
                }

                item.Total = item.Price * item.Quantity;
                SaveCarts();
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
                SaveCarts();
            }
        }

        public void Clear(int tableNumber)
        {
            lock (_lock)
            {
                GetOrCreateCart(tableNumber).Clear();
                var toRemove = _heldOrders.ToList().Where(x => x.TableNumber == tableNumber).ToList();
                foreach (var h in toRemove)
                {
                    _heldOrders.Remove(h);
                    RemoveHeldOrderFromDb(h.Id);
                }
                SaveCarts();
            }
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                _tableCarts.Clear();
                _heldOrders.Clear();
                SaveCarts();
                ClearAllHeldOrdersFromDb();
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
                return GetOrCreateCart(tableNumber).Sum(CalculateLineTotal);
            }
        }

        public decimal GetGstAmount(int tableNumber)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                return Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / MoneyPrecision.PercentDivisor)), MoneyPrecision.CurrencyDecimals);
            }
        }

        public decimal GetGrandTotal(int tableNumber)
        {
            lock (_lock)
            {
                var items = GetOrCreateCart(tableNumber);
                var subtotal = items.Sum(CalculateLineTotal);
                var gst = Math.Round(
                    items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / MoneyPrecision.PercentDivisor)),
                    MoneyPrecision.CurrencyDecimals);
                return subtotal + gst;
            }
        }

        private List<OrderItem> GetOrCreateCart(int tableNumber)
        {
            if (!_tableCarts.TryGetValue(tableNumber, out var cart))
            {
                cart = new List<OrderItem>();
                _tableCarts[tableNumber] = cart;
            }
            return cart;
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
                        Total = CalculateLineTotal(item)
                    });
                }
                SaveCarts();
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
                    SaveCarts();
                }
            }
        }

        // ── Held orders ──────────────────────────────────────────────────────

        private readonly ConcurrentList<HeldOrder> _heldOrders = new();

        public void HoldOrder(int tableNumber, string holdName)
        {
            lock (_lock)
            {
                // Get items directly from the internal cart (no lock re-entry)
                var rawItems = GetOrCreateCart(tableNumber);
                if (rawItems.Count == 0) return;

                // Snapshot the items sorted by name (same projection as GetItems)
                var snapshot = rawItems
                    .OrderBy(x => x.ItemName)
                    .Select(x => new OrderItem
                    {
                        ItemId = x.ItemId,
                        ItemName = x.ItemName,
                        Quantity = x.Quantity,
                        Price = x.Price,
                        TaxPercentage = x.TaxPercentage,
                        Total = x.Total
                    }).ToList();

                var held = new HeldOrder
                {
                    HoldName = string.IsNullOrWhiteSpace(holdName) ? $"Table {tableNumber}" : holdName,
                    HeldAt = DateTime.UtcNow,
                    TableNumber = tableNumber,
                    Items = snapshot
                };

                _heldOrders.Add(held);
                SaveHeldOrderToDb(held);

                // Clear the cart directly without re-acquiring the lock
                rawItems.Clear();
                SaveCarts();
            }
        }

        public List<HeldOrder> GetHeldOrders() => _heldOrders.ToList();

        public void ResumeHeldOrder(Guid heldOrderId, int targetTableNumber)
        {
            lock (_lock)
            {
                var held = _heldOrders.FirstOrDefault(x => x.Id == heldOrderId);
                if (held == null) return;

                // Load items directly without re-acquiring the lock
                var cart = GetOrCreateCart(targetTableNumber);
                cart.Clear();
                foreach (var item in held.Items)
                {
                    cart.Add(new OrderItem
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TaxPercentage = item.TaxPercentage,
                        Total = CalculateLineTotal(item)
                    });
                }
                SaveCarts();

                _heldOrders.Remove(held);
                RemoveHeldOrderFromDb(heldOrderId);
            }
        }

        public void TransferTable(int sourceTableNumber, int targetTableNumber)
        {
            if (sourceTableNumber == targetTableNumber) return;

            lock (_lock)
            {
                // Access the raw list directly to avoid re-entry on _lock
                var sourceItems = GetOrCreateCart(sourceTableNumber);
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

                // Clear source directly
                sourceItems.Clear();
                SaveCarts();
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

        private static decimal CalculateLineTotal(OrderItem item)
        {
            return item.Price * item.Quantity;
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
        public void Clear() { lock (_sync) _list.Clear(); }
    }
}
