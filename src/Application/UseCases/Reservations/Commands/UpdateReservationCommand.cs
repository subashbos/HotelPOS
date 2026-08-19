using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    public record UpdateReservationCommand(Reservation Reservation) : IRequest;

    public class UpdateReservationCommandHandler : IRequestHandler<UpdateReservationCommand>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IAuthorizationService _authorization;

        public UpdateReservationCommandHandler(IReservationRepository reservationRepository, ITableRepository tableRepository, IAuthorizationService authorization)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _authorization = authorization;
        }

        public async Task Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Reservation);

            var reservation = request.Reservation;

            var existing = await _reservationRepository.GetByIdAsync(reservation.Id)
                ?? throw new KeyNotFoundException($"Reservation #{reservation.Id} not found.");

            if (existing.Status == ReservationStatuses.Cancelled || existing.Status == ReservationStatuses.Completed || existing.Status == ReservationStatuses.NoShow)
                throw new InvalidOperationException($"A {existing.Status} reservation can no longer be edited.");

            var table = await _tableRepository.GetByIdAsync(reservation.TableId)
                ?? throw new ArgumentException($"Table #{reservation.TableId} does not exist.");

            if (reservation.PartySize > table.Capacity)
                throw new ArgumentException($"Party size ({reservation.PartySize}) exceeds table '{table.Name}' capacity ({table.Capacity}).");

            await ReservationOverlapChecker.EnsureNoOverlapAsync(_reservationRepository, reservation, excludeReservationId: reservation.Id);

            await _reservationRepository.UpdateAsync(reservation);
        }
    }
}
