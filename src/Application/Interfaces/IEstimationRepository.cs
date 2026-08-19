using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEstimationRepository
    {
        Task<List<Estimation>> GetEstimationsAsync();
        Task<Estimation?> GetByIdAsync(int id);
        Task AddAsync(Estimation estimation);
        Task UpdateAsync(Estimation estimation);
        Task DeleteAsync(int id);

        /// <summary>
        /// Atomically transitions the estimation from Accepted to Converted in a single guarded
        /// SQL UPDATE (via EF Core's ExecuteUpdateAsync), so two concurrent "convert to order"
        /// requests for the same estimation can't both pass a stale in-memory status check and
        /// each spawn their own order. Mirrors <see cref="IItemRepository.TryDeductStockAsync"/>.
        /// </summary>
        /// <returns>true if the row was updated (it was still Accepted); false if it was not found
        /// or was no longer in the Accepted state (already converted, or edited concurrently).</returns>
        Task<bool> TryMarkConvertedAsync(int id, int orderId);
    }
}
