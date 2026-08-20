using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
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
        private readonly IAuthorizationService _authorization;

        public RefundOrderCommandHandler(IOrderService orderService, IAuthorizationService authorization)
        {
            _orderService = orderService;
            _authorization = authorization;
        }

        public async Task Handle(RefundOrderCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.OrderManagement);

            await _orderService.RefundOrderInternalAsync(request.OrderId, request.ItemsToRefund, request.Reason);
        }
    }
}
