using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record RefundOrderCommand(
        int OrderId,
        List<OrderItemRefundDto> ItemsToRefund,
        string Reason
    ) : IRequest;

    public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly IItemService _itemService;
        private readonly IMediator _mediator;

        public RefundOrderCommandHandler(IOrderRepository repo, IItemService itemService, IMediator mediator)
        {
            _repo = repo;
            _itemService = itemService;
            _mediator = mediator;
        }

        public async Task Handle(RefundOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.ItemsToRefund == null || request.ItemsToRefund.Count == 0)
                throw new ArgumentException("No items specified for refund.", nameof(request));

            var order = await _repo.GetByIdWithItemsAsync(request.OrderId)
                ?? throw new KeyNotFoundException($"Order #{request.OrderId} not found.");

            if (order.Status == "Void")
                throw new InvalidOperationException("Cannot refund a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                decimal refundTotal = 0;

                foreach (var rItem in request.ItemsToRefund)
                {
                    var orderItem = order.Items.FirstOrDefault(x => x.ItemId == rItem.ItemId)
                        ?? throw new ArgumentException($"Item #{rItem.ItemId} not found in Order #{request.OrderId}.");

                    if (rItem.QuantityToRefund <= 0 || rItem.QuantityToRefund > orderItem.Quantity)
                        throw new ArgumentException($"Invalid refund quantity ({rItem.QuantityToRefund}) for '{orderItem.ItemName}'.");

                    await _itemService.DeductStockAsync(orderItem.ItemId, -rItem.QuantityToRefund);
                    refundTotal += orderItem.Price * rItem.QuantityToRefund;
                    orderItem.Quantity -= rItem.QuantityToRefund;
                    orderItem.Total = orderItem.Price * orderItem.Quantity;
                }

                order.Items.RemoveAll(x => x.Quantity == 0);
                CalculateTotals(order, order.Items);
                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);
                order.RefundedAmount += refundTotal;
                order.RefundReason = request.Reason;
                order.AmountPaid = Math.Max(0, order.AmountPaid - refundTotal);

                if (order.CashPaid >= refundTotal) order.CashPaid -= refundTotal;
                else
                {
                    decimal rem = refundTotal - order.CashPaid;
                    order.CashPaid = 0;
                    if (order.UpiPaid >= rem) order.UpiPaid -= rem;
                    else { order.UpiPaid = 0; order.CardPaid = Math.Max(0, order.CardPaid - rem); }
                }

                order.Status = order.Items.Count == 0 ? "Refunded" : "PartiallyRefunded";

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Refund",
                    $"Refund amount: {refundTotal:N2}. Reason: {request.Reason}"), cancellationToken);
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }

        private static void CalculateTotals(HotelPOS.Domain.Entities.Order order, List<HotelPOS.Domain.Entities.OrderItem> items)
        {
            order.Subtotal = items.Sum(x => x.Total);
            order.GstAmount = Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);
            order.CgstAmount = Math.Round(order.GstAmount / 2m, 2);
            order.SgstAmount = order.GstAmount - order.CgstAmount;
            order.IgstAmount = 0m;
        }
    }
}
