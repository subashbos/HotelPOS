using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Commands
{
    public record ProcessPartialPaymentCommand(int OrderId, decimal Cash, decimal Card, decimal Upi) : IRequest;

    public class ProcessPartialPaymentCommandHandler : IRequestHandler<ProcessPartialPaymentCommand>
    {
        private readonly IOrderRepository _repo;
        private readonly IMediator _mediator;

        public ProcessPartialPaymentCommandHandler(IOrderRepository repo, IMediator mediator)
        {
            _repo = repo;
            _mediator = mediator;
        }

        public async Task Handle(ProcessPartialPaymentCommand request, CancellationToken cancellationToken)
        {
            var order = await _repo.GetByIdWithItemsAsync(request.OrderId)
                ?? throw new KeyNotFoundException($"Order #{request.OrderId} not found.");

            if (order.Status == "Void")
                throw new InvalidOperationException("Cannot add payment to a void order.");

            await _repo.BeginTransactionAsync();
            try
            {
                order.CashPaid += request.Cash;
                order.CardPaid += request.Card;
                order.UpiPaid += request.Upi;
                order.AmountPaid = order.CashPaid + order.CardPaid + order.UpiPaid;
                order.Status = order.AmountPaid >= order.TotalAmount ? "Paid" : "Partial";

                await _repo.UpdateAsync(order);
                await _repo.CommitTransactionAsync();
                await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Payment",
                    $"Cash: {request.Cash:N2}, Card: {request.Card:N2}, UPI: {request.Upi:N2}. Paid total: {order.AmountPaid:N2}"), cancellationToken);
            }
            catch
            {
                await _repo.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
