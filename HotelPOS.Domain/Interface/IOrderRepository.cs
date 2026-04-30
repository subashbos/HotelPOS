namespace HotelPOS.Domain.Interface
{
    public interface IOrderRepository
    {
        Task<int> AddAsync(Order order);

        Task<string> GetNextInvoiceNumberAsync(string fiscalYear);

        /// <summary>Returns all orders with their line items eager-loaded.</summary>
        Task<List<Order>> GetAllWithItemsAsync();

        /// <summary>Returns a paged list of orders with optional date and table filtering.</summary>
        Task<(List<Order> Items, int TotalCount)> GetPagedWithItemsAsync(int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null, int? tableNumber = null);

        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }
}
