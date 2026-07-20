using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record ProcessPartialPaymentCommand(int OrderId, decimal Cash, decimal Card, decimal Upi) : IRequest;

    public class ProcessPartialPaymentCommandHandler : IRequestHandler<ProcessPartialPaymentCommand>
    {
        private readonly IOrderService _orderService;

        public ProcessPartialPaymentCommandHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task Handle(ProcessPartialPaymentCommand request, CancellationToken cancellationToken)
        {
            await _orderService.ProcessPartialPaymentInternalAsync(request.OrderId, request.Cash, request.Card, request.Upi);
        }
    }
}
