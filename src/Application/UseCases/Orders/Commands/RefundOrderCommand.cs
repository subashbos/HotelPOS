using HotelPOS.Application.Interfaces;
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
        private readonly IOrderService _orderService;

        public RefundOrderCommandHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Handle(RefundOrderCommand request, CancellationToken cancellationToken)
        {
            await _orderService.RefundOrderInternalAsync(request.OrderId, request.ItemsToRefund, request.Reason);
        }
    }
}
