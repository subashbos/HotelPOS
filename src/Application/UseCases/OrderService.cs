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

        /// <summary>
        /// Creates and persists a new order with the specified items, computes totals and taxes, deducts stock, and returns the created order's identifier.
        /// </summary>
        /// <param name="items">List of items to include in the order; each item must have Price &gt;= 0 and Quantity &gt; 0.</param>
        /// <param name="tableNumber">Table number for the order; required when <paramref name="orderType"/> is "DineIn". For "Takeaway" or "Online", the table number is normalized to 0.</param>
        /// <param name="discount">Discount amount applied to the order; must be &gt;= 0 and not exceed the pre-tax subtotal.</param>
        /// <param name="paymentMode">Payment mode; allowed values: "Cash", "Card", "UPI".</param>
        /// <param name="customerName">Optional customer name.</param>
        /// <param name="customerPhone">Optional customer phone number.</param>
        /// <param name="customerGstin">Optional customer GSTIN.</param>
        /// <param name="orderType">Order type; allowed values: "DineIn", "Takeaway", "Online".</param>
        /// <returns>The database identifier of the newly created order.</returns>
        /// <exception cref="ArgumentException">Thrown when input validation fails (empty items, invalid table number, negative discount, discount exceeding subtotal, invalid payment mode/order type, or invalid item price/quantity).</exception>
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

            await _repo.BeginTransactionAsync();
            try
            {
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
                order.Status = "Paid";
                order.AmountPaid = order.TotalAmount;
                if (paymentMode == "Cash") order.CashPaid = order.TotalAmount;
                else if (paymentMode == "Card") order.CardPaid = order.TotalAmount;
                else if (paymentMode == "UPI") order.UpiPaid = order.TotalAmount;

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

        public async Task VoidOrderAsync(int orderId, string reason, string authorizedUser)
        {
            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == "Void") throw new InvalidOperationException("Order is already void.");

            await _repo.BeginTransactionAsync();
            try
            {
                // Revert stock
                foreach (var item in order.Items)
                {
                    await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);
                }

                order.Status = "Void";
                order.VoidReason = reason;
                order.Subtotal = 0;
                order.GstAmount = 0;
                order.CgstAmount = 0;
                order.SgstAmount = 0;
                order.IgstAmount = 0;
                order.TotalAmount = 0;
                order.AmountPaid = 0;
                order.CashPaid = 0;
                order.CardPaid = 0;
                order.UpiPaid = 0;

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Void", $"Voided by {authorizedUser}. Reason: {reason}"));
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task RefundOrderAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason)
        {
            if (itemsToRefund == null || itemsToRefund.Count == 0)
                throw new ArgumentException("No items specified for refund.", nameof(itemsToRefund));

            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == "Void") throw new InvalidOperationException("Cannot refund a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                decimal refundTotal = 0;

                foreach (var rItem in itemsToRefund)
                {
                    var orderItem = order.Items.FirstOrDefault(x => x.ItemId == rItem.ItemId);
                    if (orderItem == null)
                        throw new ArgumentException($"Item #{rItem.ItemId} not found in Order #{orderId}.");

                    if (rItem.QuantityToRefund <= 0 || rItem.QuantityToRefund > orderItem.Quantity)
                        throw new ArgumentException($"Invalid refund quantity ({rItem.QuantityToRefund}) for '{orderItem.ItemName}'.");

                    // Restore inventory
                    await _itemService.DeductStockAsync(orderItem.ItemId, -rItem.QuantityToRefund);

                    // Compute refund value for this line
                    refundTotal += orderItem.Price * rItem.QuantityToRefund;

                    orderItem.Quantity -= rItem.QuantityToRefund;
                    orderItem.Total = orderItem.Price * orderItem.Quantity;
                }

                // Remove items completely refunded
                order.Items.RemoveAll(x => x.Quantity == 0);

                // Recalculate totals
                CalculateTotals(order, order.Items);
                
                decimal oldTotal = order.TotalAmount;
                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);
                order.RefundedAmount += refundTotal;
                order.RefundReason = reason;

                // Adjust payment split values proportionally (defaulting to CashPaid reductions first)
                order.AmountPaid = Math.Max(0, order.AmountPaid - refundTotal);
                if (order.CashPaid >= refundTotal) order.CashPaid -= refundTotal;
                else
                {
                    decimal remainder = refundTotal - order.CashPaid;
                    order.CashPaid = 0;
                    if (order.UpiPaid >= remainder) order.UpiPaid -= remainder;
                    else
                    {
                        order.UpiPaid = 0;
                        order.CardPaid = Math.Max(0, order.CardPaid - remainder);
                    }
                }

                if (order.Items.Count == 0)
                {
                    order.Status = "Refunded";
                }
                else
                {
                    order.Status = "PartiallyRefunded";
                }

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Refund", $"Refund amount: {refundTotal:N2}. Reason: {reason}"));
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task ProcessPartialPaymentAsync(int orderId, decimal cash, decimal card, decimal upi)
        {
            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == "Void") throw new InvalidOperationException("Cannot add payment to a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                order.CashPaid += cash;
                order.CardPaid += card;
                order.UpiPaid += upi;
                order.AmountPaid = order.CashPaid + order.CardPaid + order.UpiPaid;

                if (order.AmountPaid >= order.TotalAmount)
                {
                    order.Status = "Paid";
                }
                else
                {
                    order.Status = "Partial";
                }

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Payment", $"Payment added: Cash: {cash:N2}, Card: {card:N2}, UPI: {upi:N2}. Paid total: {order.AmountPaid:N2}"));
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
