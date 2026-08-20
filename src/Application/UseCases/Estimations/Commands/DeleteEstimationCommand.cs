using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Commands
{
    public record DeleteEstimationCommand(int Id) : IRequest;

    public class DeleteEstimationCommandHandler : IRequestHandler<DeleteEstimationCommand>
    {
        private readonly IEstimationRepository _estimationRepository;
        private readonly IAuthorizationService _authorization;

        public DeleteEstimationCommandHandler(IEstimationRepository estimationRepository, IAuthorizationService authorization)
        {
            _estimationRepository = estimationRepository;
            _authorization = authorization;
        }

        public async Task Handle(DeleteEstimationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureDeletePermission(PermissionModules.Estimation);

            // Idempotent delete, same convention as OrderService.DeleteOrderInternalAsync /
            // DeletePurchaseCommandHandler.
            var existing = await _estimationRepository.GetByIdAsync(request.Id);
            if (existing == null) return;

            // Mirrors UpdateEstimationCommand's guard: deleting a Converted estimation would
            // silently discard the traceability link (ConvertedOrderId) to the real order it
            // produced, even though that order itself is untouched.
            if (existing.Status == EstimationStatuses.Converted)
                throw new InvalidOperationException("A converted estimation can no longer be deleted.");

            await _estimationRepository.DeleteAsync(request.Id);
        }
    }
}
