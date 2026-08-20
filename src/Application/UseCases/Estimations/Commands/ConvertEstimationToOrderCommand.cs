using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Commands
{
    /// <summary>Returns the newly created order's ID.</summary>
    public record ConvertEstimationToOrderCommand(int EstimationId) : IRequest<int>;

    public class ConvertEstimationToOrderCommandHandler : IRequestHandler<ConvertEstimationToOrderCommand, int>
    {
        private readonly IEstimationRepository _estimationRepository;
        private readonly IOrderService _orderService;
        private readonly IAuthorizationService _authorization;

        public ConvertEstimationToOrderCommandHandler(IEstimationRepository estimationRepository, IOrderService orderService, IAuthorizationService authorization)
        {
            _estimationRepository = estimationRepository;
            _orderService = orderService;
            _authorization = authorization;
        }

        public async Task<int> Handle(ConvertEstimationToOrderCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Estimation);

            var estimation = await _estimationRepository.GetByIdAsync(request.EstimationId)
                ?? throw new KeyNotFoundException($"Estimation #{request.EstimationId} not found.");

            if (estimation.Status == EstimationStatuses.Converted)
                throw new InvalidOperationException("This estimation has already been converted to an order.");

            if (estimation.Status != EstimationStatuses.Accepted)
                throw new InvalidOperationException("Only an Accepted estimation can be converted to an order.");

            // Price/tax are re-derived from the authoritative item catalog inside SaveOrderAsync
            // (same as every other order entry point), so only ItemId/ItemName/Quantity need to
            // survive the trip here - this deliberately doesn't duplicate GST/invoice-numbering
            // logic that already lives in OrderService.
            var orderRequest = new SaveOrderRequest(
                Items: estimation.EstimationItems.Select(i => new OrderItem
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity
                }).ToList(),
                TableNumber: 0,
                Discount: estimation.TotalDiscount,
                CustomerName: estimation.CustomerName,
                CustomerPhone: estimation.CustomerPhone,
                OrderType: OrderTypes.Takeaway,
                CustomerId: estimation.CustomerId
            );

            var orderId = await _orderService.SaveOrderAsync(orderRequest);

            // Atomic compare-and-swap: only succeeds if the estimation is still Accepted. If a
            // concurrent request already converted it (or edited it away from Accepted) between
            // the check above and here, we lose the race - the order we just created is orphaned,
            // so void it (returns stock) rather than leaving a duplicate/dangling order behind.
            var claimed = await _estimationRepository.TryMarkConvertedAsync(request.EstimationId, orderId);
            if (!claimed)
            {
                try
                {
                    await _orderService.DeleteOrderAsync(orderId);
                }
                catch (Exception cleanupEx) // NOSONAR: intentional - best-effort compensation; the real error below must still surface
                {
                    Serilog.Log.Error(cleanupEx, "Failed to void orphaned order #{OrderId} after losing the estimation-conversion race", orderId);
                }
                throw new InvalidOperationException("This estimation has already been converted to an order.");
            }

            return orderId;
        }
    }
}
