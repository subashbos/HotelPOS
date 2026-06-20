using MediatR;
using HotelPOS.Application.Interfaces;
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

        public VoidOrderCommandHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Handle(VoidOrderCommand request, CancellationToken cancellationToken)
        {
            await _orderService.VoidOrderInternalAsync(
                request.OrderId,
                request.Reason,
                request.AuthorizedUser
            );
        }
    }
}
