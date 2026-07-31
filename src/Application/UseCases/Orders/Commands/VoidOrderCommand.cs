using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record VoidOrderCommand(
        int OrderId,
        string Reason,
        string AuthorizedUser
    ) : IRequest;

    public class VoidOrderCommandHandler : IRequestHandler<VoidOrderCommand>
    {
        private readonly IOrderService _orderService;
        private readonly IAuthorizationService _authorization;

        public VoidOrderCommandHandler(IOrderService orderService, IAuthorizationService authorization)
        {
            _orderService = orderService;
            _authorization = authorization;
        }

        public async Task Handle(VoidOrderCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.OrderManagement);

            await _orderService.VoidOrderInternalAsync(
                request.OrderId,
                request.Reason,
                request.AuthorizedUser
            );
        }
    }
}
