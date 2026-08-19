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
            _authorization.EnsureEditPermission(PermissionModules.Reservation);

            var reservation = request.Reservation;

            var table = await _tableRepository.GetByIdAsync(reservation.TableId)
                ?? throw new ArgumentException($"Table #{reservation.TableId} does not exist.");

            if (reservation.PartySize > table.Capacity)
                throw new ArgumentException($"Party size ({reservation.PartySize}) exceeds table '{table.Name}' capacity ({table.Capacity}).");

            await _reservationRepository.BeginTransactionAsync();
            try
            {
                // Serializes concurrent bookings for this table/date so two requests can't both
                // pass the overlap check below and double-book the table.
                await _reservationRepository.AcquireTableDateLockAsync(reservation.TableId, reservation.ReservationDate.Date);

                await ReservationOverlapChecker.EnsureNoOverlapAsync(_reservationRepository, reservation);

                reservation.Status = ReservationStatuses.Reserved;
                reservation.CreatedAt = DateTime.UtcNow;
                await _reservationRepository.AddAsync(reservation);

                await _reservationRepository.CommitTransactionAsync();
            }
            catch (Exception ex) // NOSONAR: intentional - log with operation context at failure site, preserve stack trace for global handler
            {
                Serilog.Log.Error(ex, "Transaction failed while saving reservation");
                try
                {
                    await _reservationRepository.RollbackTransactionAsync();
                }
                catch (Exception rollbackEx)
                {
                    Serilog.Log.Error(rollbackEx, "Transaction rollback failed while saving reservation");
                    throw new AggregateException("Transaction failed and rollback also failed.", ex, rollbackEx);
                }
                throw;
            }
        }
    }
}
