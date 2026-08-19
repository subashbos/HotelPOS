using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    public record ChangeReservationStatusCommand(int Id, string NewStatus) : IRequest;

    public class ChangeReservationStatusCommandHandler : IRequestHandler<ChangeReservationStatusCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IAuthorizationService _authorization;

        public ChangeReservationStatusCommandHandler(IReservationRepository reservationRepository, IAuthorizationService authorization)
        {
            _reservationRepository = reservationRepository;
            _authorization = authorization;
        }

        public async Task Handle(ChangeReservationStatusCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Reservation);

            if (!ReservationStatuses.All.Contains(request.NewStatus))
                throw new ArgumentException($"'{request.NewStatus}' is not a valid reservation status.");

            var reservation = await _reservationRepository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Reservation #{request.Id} not found.");

            var allowed = ReservationStatuses.NextStatuses.GetValueOrDefault(reservation.Status, Array.Empty<string>());
            if (!allowed.Contains(request.NewStatus))
                throw new InvalidOperationException(
                    $"Cannot move a {reservation.Status} reservation to {request.NewStatus}.");

            await _reservationRepository.UpdateStatusAsync(request.Id, request.NewStatus);
        }
    }
}
