using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IReservationService
    {
        Task<List<Reservation>> GetReservationsAsync(DateTime? date = null);
        Task<Reservation?> GetReservationByIdAsync(int id);
        Task SaveReservationAsync(Reservation reservation);
        Task UpdateReservationAsync(Reservation reservation);
        Task DeleteReservationAsync(int id);

        /// <summary>Transitions a reservation's status, enforcing the allow-list in
        /// <see cref="Domain.Common.Constants.ReservationStatuses.NextStatuses"/>.</summary>
        Task ChangeStatusAsync(int id, string newStatus);
    }
}
