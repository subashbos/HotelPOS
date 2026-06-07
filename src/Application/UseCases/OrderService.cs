using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IMediator _mediator;
        private readonly IItemService _itemService;

        public OrderService(IOrderRepository repo, IMediator mediator, IItemService itemService)
        {
            _repo = repo;
            _mediator = mediator;
            _itemService = itemService;
        }

        public async Task<int> SaveOrderAsync(List<OrderItem> items, int tableNumber, decimal discount = 0, string paymentMode = "Cash", string? customerName = null, string? customerPhone = null, string? customerGstin = null, string orderType = "DineIn")
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.", nameof(items));

            if (discount < 0)
                throw new ArgumentException("Discount cannot be negative.", nameof(discount));

            // ── Financial guard: discount cannot exceed the pre-tax subtotal ──
            var preCheckSubtotal = items.Sum(x => x.Price * x.Quantity);
            if (discount > preCheckSubtotal)
                throw new ArgumentException(
                    $"Discount (₹{discount:N2}) cannot exceed order subtotal (₹{preCheckSubtotal:N2}).",
                    nameof(discount));

            var allowedModes = new[] { "Cash", "Card", "UPI" };
            if (!allowedModes.Contains(paymentMode))
                throw new ArgumentException($"Invalid payment mode. Allowed: {string.Join(", ", allowedModes)}", nameof(paymentMode));

            var allowedTypes = new[] { "DineIn", "Takeaway", "Online" };
            if (!allowedTypes.Contains(orderType))
                throw new ArgumentException($"Invalid order type. Allowed: {string.Join(", ", allowedTypes)}", nameof(orderType));

            // DineIn requires a real table; Takeaway/Online use virtual table 0
            bool requiresTable = orderType == "DineIn";
            if (requiresTable && tableNumber <= 0)
                throw new ArgumentException("Invalid table number.", nameof(tableNumber));

            // For Takeaway/Online, normalise to 0 regardless of what was passed
            int effectiveTableNumber = requiresTable ? tableNumber : 0;

            var orderItems = items
                .Select(x =>
                {
                    if (x.Price < 0) throw new ArgumentException($"Item '{x.ItemName}' cannot have a negative price.");
                    if (x.Quantity <= 0) throw new ArgumentException($"Item '{x.ItemName}' must have a quantity of at least 1.");

                    return new OrderItem
                    {
                        ItemId = x.ItemId,
                        ItemName = x.ItemName,
                        Quantity = x.Quantity,
                        Price = x.Price,
                        TaxPercentage = x.TaxPercentage,
                        Total = x.Total
                    };
                })
                .ToList();

            var now = DateTime.UtcNow;
            var fy = GetFiscalYear(now.ToLocalTime());
            var inv = await _repo.GetNextInvoiceNumberAsync(fy);

            var order = new Order
            {
                InvoiceNumber = inv,
                FiscalYear = fy,
                CreatedAt = now,
                TableNumber = effectiveTableNumber,
                Items = orderItems
            };

            CalculateTotals(order, orderItems);
            order.DiscountAmount = discount;
            order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - discount);
            order.PaymentMode = paymentMode;
            order.OrderType = orderType;
            order.CustomerName = customerName;
            order.CustomerPhone = customerPhone;
            order.CustomerGstin = customerGstin;

            await _repo.BeginTransactionAsync();
            try
            {
                var orderId = await _repo.AddAsync(order);

                // Deduct Stock
                foreach (var item in orderItems)
                {
                    await _itemService.DeductStockAsync(item.ItemId, item.Quantity);
                }

                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", orderId, "Create", $"Total: {order.TotalAmount:N2}, Table: {effectiveTableNumber}, Type: {orderType}"));
                return orderId;
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        private void CalculateTotals(Order order, List<OrderItem> items)
        {
            order.Subtotal = items.Sum(x => x.Total);
            order.GstAmount = Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);

            // Assume Intrastate default for Hotel POS (CGST = 50%, SGST = 50%)
            order.CgstAmount = Math.Round(order.GstAmount / 2m, 2);
            order.SgstAmount = order.GstAmount - order.CgstAmount;
            order.IgstAmount = 0m;
        }

        private string GetFiscalYear(DateTime date)
        {
            // India: April 1 to March 31
            int year = date.Month < 4 ? date.Year - 1 : date.Year;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        public Task<List<Order>> GetAllOrdersWithItemsAsync()
            => _repo.GetAllWithItemsAsync();

        public Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(int pageNumber, int pageSize, 
            DateTime? from = null, DateTime? to = null, int? tableNumber = null,
            string? search = null, string? paymentMode = null, string? orderType = null, int? categoryId = null)
            => _repo.GetPagedWithItemsAsync(pageNumber, pageSize, from, to, tableNumber, search, paymentMode, orderType, categoryId);

        public Task<Order?> GetOrderAsync(int id) => _repo.GetByIdWithItemsAsync(id);

        public async Task UpdateOrderAsync(Order order)
        {
            if (order.Items == null || order.Items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.");

            var oldOrder = await _repo.GetByIdWithItemsAsync(order.Id);
            if (oldOrder == null) throw new KeyNotFoundException($"Order #{order.Id} not found.");

            // Normalise table number: Takeaway/Online always store 0
            if (order.OrderType == "Takeaway" || order.OrderType == "Online")
                order.TableNumber = 0;

            await _repo.BeginTransactionAsync();
            try
            {
                // Stock Reconciliation
                var oldMap = oldOrder.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
                var newMap = order.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                // Return all old stock first (using negative to indicate return)
                foreach (var kvp in oldMap)
                {
                    await _itemService.DeductStockAsync(kvp.Key, -kvp.Value);
                }

                // Deduct all new stock
                foreach (var kvp in newMap)
                {
                    await _itemService.DeductStockAsync(kvp.Key, kvp.Value);
                }

                var oldTotal = oldOrder.TotalAmount;

                CalculateTotals(order, order.Items);
                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Update", $"Old Total: {oldTotal:N2} -> New Total: {order.TotalAmount:N2}"));
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }
        public async Task DeleteOrderAsync(int orderId)
        {
            var existing = await _repo.GetByIdWithItemsAsync(orderId);
            if (existing != null)
            {
                foreach (var item in existing.Items)
                {
                    await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);
                }

                await _repo.DeleteAsync(orderId);
                await _mediator.Publish(new EntityActionEvent("Order", orderId, "Delete", "Soft Deleted"));
            }
        }
    }
}
