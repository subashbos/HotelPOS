using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface IOrderService
    {
        Task<int> SaveOrderAsync(List<OrderItem> items, int tableNumber, decimal discount = 0, string paymentMode = "Cash", string? customerName = null, string? customerPhone = null, string? customerGstin = null);
        Task<List<Order>> GetAllOrdersWithItemsAsync();
        Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null, int? tableNumber = null);
        Task<Order?> GetOrderAsync(int id);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int orderId);
    }
}
