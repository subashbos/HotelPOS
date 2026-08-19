using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    public record DeleteReservationCommand(int Id) : IRequest;

    public class DeleteReservationCommandHandler : IRequestHandler<DeleteReservationCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IAuthorizationService _authorization;

        public DeleteReservationCommandHandler(IReservationRepository reservationRepository, IAuthorizationService authorization)
        {
            _reservationRepository = reservationRepository;
            _authorization = authorization;
        }

        public async Task Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureDeletePermission(PermissionModules.Reservation);

            // Idempotent delete, same convention as OrderService.DeleteOrderInternalAsync /
            // DeletePurchaseCommandHandler / DeleteEstimationCommandHandler.
            await _reservationRepository.DeleteAsync(request.Id);
        }
    }
}
