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

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        /// <summary>
        /// Serializes concurrent booking attempts for the same table/date using a SQL Server
        /// application lock (no-op on other providers), so the overlap check that follows it -
        /// and the AddAsync/UpdateAsync that follows that - can't race with another request doing
        /// the same for the same table/date and produce a double-booking. Must be called within
        /// an active transaction (see <see cref="BeginTransactionAsync"/>); the lock releases
        /// automatically on commit/rollback. Mirrors <c>IOrderRepository.GetNextInvoiceNumberAsync</c>.
        /// </summary>
        Task AcquireTableDateLockAsync(int tableId, DateTime date);
    }
}
