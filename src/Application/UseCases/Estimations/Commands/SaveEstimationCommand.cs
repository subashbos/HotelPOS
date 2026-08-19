using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Commands
{
    public record SaveEstimationCommand(Estimation Estimation) : IRequest;

    public class SaveEstimationCommandHandler : IRequestHandler<SaveEstimationCommand>
    {
        private readonly IEstimationRepository _estimationRepository;
        private readonly IAuthorizationService _authorization;

        public SaveEstimationCommandHandler(IEstimationRepository estimationRepository, IAuthorizationService authorization)
        {
            _estimationRepository = estimationRepository;
            _authorization = authorization;
        }

        public async Task Handle(SaveEstimationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Estimation);

            await _estimationRepository.AddAsync(request.Estimation);
        }
    }
}
