using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<int> AddAsync(Order order);

        Task<string> GetNextInvoiceNumberAsync(string fiscalYear);

        /// <summary>Returns all orders with their line items eager-loaded.</summary>
        Task<List<Order>> GetAllWithItemsAsync();

        /// <summary>Returns a paged list of orders with advanced filtering.</summary>
        Task<(List<Order> Items, int TotalCount)> GetPagedWithItemsAsync(int pageNumber, int pageSize,
            OrderQueryFilter? filter = null, CancellationToken cancellationToken = default);

        Task UpdateAsync(Order order);
        Task<Order?> GetByIdWithItemsAsync(int id);
        Task DeleteAsync(int id);

        // ── Transactions ──────────────────────────────────────────────────────
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

    /// <summary>Optional filter criteria for <see cref="IOrderRepository.GetPagedWithItemsAsync"/>.</summary>
    public record OrderQueryFilter(
        DateTime? From = null,
        DateTime? To = null,
        int? TableNumber = null,
        string? Search = null,
        string? PaymentMode = null,
        string? OrderType = null,
        int? CategoryId = null,
        int? CustomerId = null
    );
}
