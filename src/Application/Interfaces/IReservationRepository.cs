using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IReservationRepository
    {
        Task<List<Reservation>> GetReservationsAsync(DateTime? date = null);

        /// <summary>Non-cancelled reservations for the given table/date, used for overlap-conflict
        /// checks. <paramref name="excludeReservationId"/> excludes the reservation being edited
        /// from its own conflict check.</summary>
        Task<List<Reservation>> GetActiveReservationsForTableAsync(int tableId, DateTime date, int? excludeReservationId = null);

        Task<Reservation?> GetByIdAsync(int id);
        Task AddAsync(Reservation reservation);

        /// <summary>Updates booking details only (table, customer, date/time window, party size,
        /// notes) - never touches <see cref="Reservation.Status"/>, which is exclusively managed by
        /// <c>ChangeReservationStatusCommandHandler</c>.</summary>
        Task UpdateAsync(Reservation reservation);

        Task UpdateStatusAsync(int id, string status);
        Task DeleteAsync(int id);
    }
}
