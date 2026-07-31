using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record UpdateOrderCommand(Order Order) : IRequest;

    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
    {
        private readonly IOrderService _orderService;
        private readonly IAuthorizationService _authorization;

        public UpdateOrderCommandHandler(IOrderService orderService, IAuthorizationService authorization)
        {
            _orderService = orderService;
            _authorization = authorization;
        }

        public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.OrderManagement);

            await _orderService.UpdateOrderInternalAsync(request.Order);
        }
    }
}
