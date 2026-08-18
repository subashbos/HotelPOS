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

        /// <summary>
        /// Returns per-item sales totals for the date range, aggregated in SQL rather than
        /// loading every matching order+item row into memory - used by the Item Report.
        /// </summary>
        Task<List<ItemSalesAggregate>> GetItemSalesAggregateAsync(DateTime? from, DateTime? to);

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

    /// <summary>One row of <see cref="IOrderRepository.GetItemSalesAggregateAsync"/>'s SQL-side aggregation.</summary>
    public record ItemSalesAggregate(string ItemName, int TotalQtySold, decimal TotalRevenue, decimal AverageUnitPrice);
}
