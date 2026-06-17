using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record DeleteOrderCommand(int OrderId) : IRequest;

    public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly IItemService _itemService;
        private readonly IMediator _mediator;

        public DeleteOrderCommandHandler(IOrderRepository repo, IItemService itemService, IMediator mediator)
        {
            _repo = repo;
            _itemService = itemService;
            _mediator = mediator;
        }

        public async Task Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetByIdWithItemsAsync(request.OrderId);
            if (existing == null) return;   // idempotent

            foreach (var item in existing.Items)
                await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);

            await _repo.DeleteAsync(request.OrderId);
            await _mediator.Publish(new EntityActionEvent("Order", request.OrderId, "Delete", "Soft Deleted"), cancellationToken);
        }
    }
}
