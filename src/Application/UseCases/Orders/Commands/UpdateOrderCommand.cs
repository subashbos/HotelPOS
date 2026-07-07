using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record UpdateOrderCommand(Order Order) : IRequest;

    public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
    {
        private readonly IOrderService _orderService;

        public UpdateOrderCommandHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
        {
            await _orderService.UpdateOrderInternalAsync(request.Order);
        }
    }
}
