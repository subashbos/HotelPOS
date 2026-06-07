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
            DateTime? from = null, DateTime? to = null, int? tableNumber = null, 
            string? search = null, string? paymentMode = null, string? orderType = null, int? categoryId = null);

        Task UpdateAsync(Order order);
        Task<Order?> GetByIdWithItemsAsync(int id);
        Task DeleteAsync(int id);

        // ── Transactions ──────────────────────────────────────────────────────
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
