using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record CreateOrderCommand(
        List<OrderItem> Items,
        int TableNumber,
        decimal Discount = 0,
        string PaymentMode = "Cash",
        string? CustomerName = null,
        string? CustomerPhone = null,
        string? CustomerGstin = null,
        string OrderType = "DineIn"
    ) : IRequest<int>;

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
    {
        private readonly IOrderService _orderService;

        public CreateOrderCommandHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            return await _orderService.SaveOrderInternalAsync(new SaveOrderRequest(
                request.Items,
                request.TableNumber,
                request.Discount,
                request.PaymentMode,
                request.CustomerName,
                request.CustomerPhone,
                request.CustomerGstin,
                request.OrderType
            ));
        }
    }
}
