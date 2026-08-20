using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface IItemRepository
    {
        Task<List<Item>> GetAllAsync();
        Task<Item?> GetByIdAsync(int id);
        Task<List<Item>> GetByIdsAsync(List<int> ids);
        Task<int> AddAsync(Item item);
        Task AddRangeAsync(List<Item> items);
        Task UpdateAsync(Item item);
        Task UpdateRangeAsync(List<Item> items);
        Task DeleteAsync(int id);

        /// <summary>
        /// Atomically decrements StockQuantity by <paramref name="quantity"/> in a single guarded
        /// SQL UPDATE (via EF Core's ExecuteUpdateAsync) so concurrent callers can't both pass a
        /// stale in-memory stock check and oversell the last units. A non-positive quantity (stock
        /// being returned, e.g. on void/refund) always succeeds. Callers are responsible for
        /// deciding whether the item should be tracked at all (TrackInventory) before calling this.
        /// </summary>
        /// <returns>true if the row was updated (stock was sufficient or quantity was non-positive); false if the item wasn't found or stock was insufficient.</returns>
        Task<bool> TryDeductStockAsync(int itemId, int quantity);

        /// <summary>
        /// Atomically adds <paramref name="delta"/> (positive or negative) to StockQuantity in a
        /// single guarded SQL UPDATE (via EF Core's ExecuteUpdateAsync), clamped to zero so a
        /// negative delta can't drive stock below zero. Unlike a read-modify-write on a tracked
        /// entity, this can't lose a concurrent purchase's stock credit to a last-writer-wins
        /// overwrite - the same class of bug <see cref="TryDeductStockAsync"/> already guards
        /// against on the order-deduction side. Callers are responsible for deciding whether the
        /// item should be tracked at all (TrackInventory) before calling this.
        /// </summary>
        Task AdjustStockAsync(int itemId, int delta);
    }
}
