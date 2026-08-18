using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    public record SaveReservationCommand(Reservation Reservation) : IRequest;

    public class SaveReservationCommandHandler : IRequestHandler<SaveReservationCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IAuthorizationService _authorization;

        public SaveReservationCommandHandler(IReservationRepository reservationRepository, ITableRepository tableRepository, IAuthorizationService authorization)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _authorization = authorization;
        }

        public async Task Handle(SaveReservationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Reservation);

            var reservation = request.Reservation;

            var table = await _tableRepository.GetByIdAsync(reservation.TableId)
                ?? throw new ArgumentException($"Table #{reservation.TableId} does not exist.");

            if (reservation.PartySize > table.Capacity)
                throw new ArgumentException($"Party size ({reservation.PartySize}) exceeds table '{table.Name}' capacity ({table.Capacity}).");

            await ReservationOverlapChecker.EnsureNoOverlapAsync(_reservationRepository, reservation);

            reservation.Status = ReservationStatuses.Reserved;
            reservation.CreatedAt = DateTime.UtcNow;
            await _reservationRepository.AddAsync(reservation);
        }
    }
}
