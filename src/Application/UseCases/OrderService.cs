using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using HotelPOS.Application.UseCases.Orders.Commands;
using FluentValidation;

namespace HotelPOS.Application.UseCases
{
    public class OrderService : IOrderService
    {
        private const string OrderEntityType = "Order";

        private readonly IOrderRepository _repo;
        private readonly IMediator? _mediator;
        private readonly IItemService _itemService;
        private readonly IValidator<CreateOrderCommand> _validator;
        private readonly IBomService? _bomService;

        public OrderService(IOrderRepository repo, IMediator? mediator, IItemService itemService, IValidator<CreateOrderCommand>? validator = null, IBomService? bomService = null)
        {
            _repo = repo;
            _mediator = mediator;
            _itemService = itemService;
            _validator = validator ?? new CreateOrderCommandValidator();
            _bomService = bomService;
        }

        public Task<int> SaveOrderAsync(SaveOrderRequest request) => SaveOrderInternalAsync(request);

        public async Task<int> SaveOrderInternalAsync(SaveOrderRequest request)
        {
            var (items, tableNumber, discount, paymentMode, customerName, customerPhone, customerGstin, orderType) = request;

            var command = new CreateOrderCommand(
                items,
                tableNumber,
                discount,
                paymentMode,
                customerName,
                customerPhone,
                customerGstin,
                orderType
            );

            var valResult = _validator.Validate(command);
            if (!valResult.IsValid)
            {
                var error = valResult.Errors[0];
                throw new ArgumentException(error.ErrorMessage);
            }

            // DineIn requires a real table; Takeaway/Online use virtual table 0
            bool requiresTable = orderType == OrderTypes.DineIn;

            // For Takeaway/Online, normalise to 0 regardless of what was passed
            int effectiveTableNumber = requiresTable ? tableNumber : 0;

            var orderItems = items
                .Select(x =>
                {
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
                order.Status = OrderStatuses.Paid;
                order.AmountPaid = order.TotalAmount;
                if (paymentMode == PaymentModes.Cash) order.CashPaid = order.TotalAmount;
                else if (paymentMode == PaymentModes.Card) order.CardPaid = order.TotalAmount;
                else if (paymentMode == PaymentModes.Upi) order.UpiPaid = order.TotalAmount;

                var orderId = await _repo.AddAsync(order);

                // Deduct Stock
                foreach (var item in orderItems)
                {
                    await _itemService.DeductStockAsync(item.ItemId, item.Quantity);
                    if (_bomService != null) await _bomService.DeductIngredientStockAsync(item.ItemId, item.Quantity);
                }

                await _repo.CommitTransactionAsync();
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, orderId, "Create", $"Total: {order.TotalAmount:N2}, Table: {effectiveTableNumber}, Type: {orderType}"));
                }
                return orderId;
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        private static void CalculateTotals(Order order, List<OrderItem> items)
        {
            order.Subtotal = items.Sum(x => x.Total);
            order.GstAmount = Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / MoneyPrecision.PercentDivisor)), MoneyPrecision.CurrencyDecimals);

            // Assume Intrastate default for Hotel POS (CGST = 50%, SGST = 50%)
            order.CgstAmount = Math.Round(order.GstAmount / 2m, MoneyPrecision.CurrencyDecimals);
            order.SgstAmount = order.GstAmount - order.CgstAmount;
            order.IgstAmount = 0m;
        }

        private static string GetFiscalYear(DateTime date)
        {
            // India: April 1 to March 31
            int year = date.Month < 4 ? date.Year - 1 : date.Year;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        public Task<List<Order>> GetAllOrdersWithItemsAsync()
            => _repo.GetAllWithItemsAsync();

        public Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(PagedOrdersRequest request, CancellationToken cancellationToken = default)
            => _repo.GetPagedWithItemsAsync(request.PageNumber, request.PageSize, request.From, request.To, request.TableNumber, request.Search, request.PaymentMode, request.OrderType, request.CategoryId, cancellationToken);

        public Task<Order?> GetOrderAsync(int id) => _repo.GetByIdWithItemsAsync(id);

        public Task UpdateOrderAsync(Order order) => UpdateOrderInternalAsync(order);

        public async Task UpdateOrderInternalAsync(Order order)
        {
            if (order.Items == null || order.Items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.");

            var oldOrder = await _repo.GetByIdWithItemsAsync(order.Id);
            if (oldOrder == null) throw new KeyNotFoundException($"Order #{order.Id} not found.");

            // Normalise table number: Takeaway/Online always store 0
            if (order.OrderType == OrderTypes.Takeaway || order.OrderType == OrderTypes.Online)
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
                    if (_bomService != null) await _bomService.DeductIngredientStockAsync(kvp.Key, -kvp.Value);
                }

                // Deduct all new stock
                foreach (var kvp in newMap)
                {
                    await _itemService.DeductStockAsync(kvp.Key, kvp.Value);
                    if (_bomService != null) await _bomService.DeductIngredientStockAsync(kvp.Key, kvp.Value);
                }

                var oldTotal = oldOrder.TotalAmount;

                CalculateTotals(order, order.Items);
                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, order.Id, "Update", $"Old Total: {oldTotal:N2} -> New Total: {order.TotalAmount:N2}"));
                }
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        public Task DeleteOrderAsync(int orderId) => DeleteOrderInternalAsync(orderId);

        public async Task DeleteOrderInternalAsync(int orderId)
        {
            var existing = await _repo.GetByIdWithItemsAsync(orderId);
            if (existing != null)
            {
                foreach (var item in existing.Items)
                {
                    await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);
                    if (_bomService != null) await _bomService.DeductIngredientStockAsync(item.ItemId, -item.Quantity);
                }

                await _repo.DeleteAsync(orderId);
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, orderId, "Delete", "Soft Deleted"));
                }
            }
        }

        public Task VoidOrderAsync(int orderId, string reason, string authorizedUser) => VoidOrderInternalAsync(orderId, reason, authorizedUser);

        public async Task VoidOrderInternalAsync(int orderId, string reason, string authorizedUser)
        {
            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == OrderStatuses.Void) throw new InvalidOperationException("Order is already void.");

            await _repo.BeginTransactionAsync();
            try
            {
                // Revert stock
                foreach (var item in order.Items)
                {
                    await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);
                    if (_bomService != null) await _bomService.DeductIngredientStockAsync(item.ItemId, -item.Quantity);
                }

                order.Status = OrderStatuses.Void;
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
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, order.Id, "Void", $"Voided by {authorizedUser}. Reason: {reason}"));
                }
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        public Task RefundOrderAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason) => RefundOrderInternalAsync(orderId, itemsToRefund, reason);

        public async Task RefundOrderInternalAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason)
        {
            if (itemsToRefund == null || itemsToRefund.Count == 0)
                throw new ArgumentException("No items specified for refund.", nameof(itemsToRefund));

            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == OrderStatuses.Void) throw new InvalidOperationException("Cannot refund a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                decimal refundTotal = await RefundItemsAsync(order, itemsToRefund);

                // Remove items completely refunded
                order.Items.RemoveAll(x => x.Quantity == 0);

                // Recalculate totals
                CalculateTotals(order, order.Items);

                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);
                order.RefundedAmount += refundTotal;
                order.RefundReason = reason;

                // Adjust payment split values proportionally (defaulting to CashPaid reductions first)
                ApplyRefundToPaymentSplit(order, refundTotal);

                order.Status = order.Items.Count == 0 ? OrderStatuses.Refunded : OrderStatuses.PartiallyRefunded;

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, order.Id, "Refund", $"Refund amount: {refundTotal:N2}. Reason: {reason}"));
                }
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task<decimal> RefundItemsAsync(Order order, List<OrderItemRefundDto> itemsToRefund)
        {
            decimal refundTotal = 0;

            foreach (var rItem in itemsToRefund)
            {
                var orderItem = order.Items.FirstOrDefault(x => x.ItemId == rItem.ItemId);
                if (orderItem == null)
                    throw new ArgumentException($"Item #{rItem.ItemId} not found in Order #{order.Id}.");

                if (rItem.QuantityToRefund <= 0 || rItem.QuantityToRefund > orderItem.Quantity)
                    throw new ArgumentException($"Invalid refund quantity ({rItem.QuantityToRefund}) for '{orderItem.ItemName}'.");

                // Restore inventory
                await _itemService.DeductStockAsync(orderItem.ItemId, -rItem.QuantityToRefund);
                if (_bomService != null) await _bomService.DeductIngredientStockAsync(orderItem.ItemId, -rItem.QuantityToRefund);

                // Compute refund value for this line
                refundTotal += orderItem.Price * rItem.QuantityToRefund;

                orderItem.Quantity -= rItem.QuantityToRefund;
                orderItem.Total = orderItem.Price * orderItem.Quantity;
            }

            return refundTotal;
        }

        private static void ApplyRefundToPaymentSplit(Order order, decimal refundTotal)
        {
            order.AmountPaid = Math.Max(0, order.AmountPaid - refundTotal);
            if (order.CashPaid >= refundTotal)
            {
                order.CashPaid -= refundTotal;
                return;
            }

            decimal remainder = refundTotal - order.CashPaid;
            order.CashPaid = 0;
            if (order.UpiPaid >= remainder)
            {
                order.UpiPaid -= remainder;
                return;
            }

            order.UpiPaid = 0;
            order.CardPaid = Math.Max(0, order.CardPaid - remainder);
        }

        public Task ProcessPartialPaymentAsync(int orderId, decimal cash, decimal card, decimal upi) => ProcessPartialPaymentInternalAsync(orderId, cash, card, upi);

        public async Task ProcessPartialPaymentInternalAsync(int orderId, decimal cash, decimal card, decimal upi)
        {
            var order = await _repo.GetByIdWithItemsAsync(orderId);
            if (order == null) throw new KeyNotFoundException($"Order #{orderId} not found.");
            if (order.Status == OrderStatuses.Void) throw new InvalidOperationException("Cannot add payment to a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                order.CashPaid += cash;
                order.CardPaid += card;
                order.UpiPaid += upi;
                order.AmountPaid = order.CashPaid + order.CardPaid + order.UpiPaid;

                if (order.AmountPaid >= order.TotalAmount)
                {
                    order.Status = OrderStatuses.Paid;
                }
                else
                {
                    order.Status = OrderStatuses.Partial;
                }

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                if (_mediator != null)
                {
                    await _mediator.Publish(new EntityActionEvent(OrderEntityType, order.Id, "Payment", $"Payment added: Cash: {cash:N2}, Card: {card:N2}, UPI: {upi:N2}. Paid total: {order.AmountPaid:N2}"));
                }
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
