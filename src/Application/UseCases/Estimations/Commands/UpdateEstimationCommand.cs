using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Commands
{
    public record UpdateEstimationCommand(Estimation Estimation) : IRequest;

    public class UpdateEstimationCommandHandler : IRequestHandler<UpdateEstimationCommand>
    {
        private readonly IEstimationRepository _estimationRepository;
        private readonly IAuthorizationService _authorization;

        public UpdateEstimationCommandHandler(IEstimationRepository estimationRepository, IAuthorizationService authorization)
        {
            _estimationRepository = estimationRepository;
            _authorization = authorization;
        }

        public async Task Handle(UpdateEstimationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Estimation);

            var existing = await _estimationRepository.GetByIdAsync(request.Estimation.Id)
                ?? throw new KeyNotFoundException($"Estimation #{request.Estimation.Id} not found.");

            if (existing.Status == EstimationStatuses.Converted)
                throw new InvalidOperationException("A converted estimation can no longer be edited.");

            if (request.Estimation.Status == EstimationStatuses.Converted)
                throw new InvalidOperationException("Use the convert-to-order action to mark an estimation as Converted.");

            await _estimationRepository.UpdateAsync(request.Estimation);
        }
    }
}
