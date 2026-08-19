using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases.Reservations.Commands
{
    /// <summary>Shared overlap-conflict check used by both <see cref="SaveReservationCommandHandler"/>
    /// and <see cref="UpdateReservationCommandHandler"/>.</summary>
    internal static class ReservationOverlapChecker
    {
        public static async Task EnsureNoOverlapAsync(
            IReservationRepository repository, Reservation reservation, int? excludeReservationId = null)
        {
            var existing = await repository.GetActiveReservationsForTableAsync(
                reservation.TableId, reservation.ReservationDate.Date, excludeReservationId);

            bool Overlaps(Reservation other) =>
                reservation.StartTime < other.EndTime && other.StartTime < reservation.EndTime;

            if (existing.Any(Overlaps))
                throw new InvalidOperationException(
                    $"This table is already booked for an overlapping time slot on {reservation.ReservationDate:d}.");
        }
    }
}
