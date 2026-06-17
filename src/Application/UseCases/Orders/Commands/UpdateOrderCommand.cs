using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record UpdateOrderCommand(Order Order) : IRequest;

    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly IItemService _itemService;
        private readonly IMediator _mediator;

        public UpdateOrderCommandHandler(IOrderRepository repo, IItemService itemService, IMediator mediator)
        {
            _repo = repo;
            _itemService = itemService;
            _mediator = mediator;
        }

        public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = request.Order;

            if (order.Items == null || order.Items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.");

            var oldOrder = await _repo.GetByIdWithItemsAsync(order.Id)
                ?? throw new KeyNotFoundException($"Order #{order.Id} not found.");

            if (order.OrderType == "Takeaway" || order.OrderType == "Online")
                order.TableNumber = 0;

            await _repo.BeginTransactionAsync();
            try
            {
                var oldMap = oldOrder.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
                var newMap = order.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                foreach (var kvp in oldMap)
                    await _itemService.DeductStockAsync(kvp.Key, -kvp.Value);

                foreach (var kvp in newMap)
                    await _itemService.DeductStockAsync(kvp.Key, kvp.Value);

                var oldTotal = oldOrder.TotalAmount;
                CalculateTotals(order, order.Items);
                order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Update",
                    $"Old Total: {oldTotal:N2} -> New Total: {order.TotalAmount:N2}"), cancellationToken);
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
            order.GstAmount = Math.Round(items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);
            order.CgstAmount = Math.Round(order.GstAmount / 2m, 2);
            order.SgstAmount = order.GstAmount - order.CgstAmount;
            order.IgstAmount = 0m;
        }
    }
}
