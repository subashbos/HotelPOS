using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<int> SaveOrderAsync(List<OrderItem> items, int tableNumber, decimal discount = 0, string paymentMode = "Cash", string? customerName = null, string? customerPhone = null, string? customerGstin = null, string orderType = "DineIn");
        Task<List<Order>> GetAllOrdersWithItemsAsync();
        Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(int pageNumber, int pageSize, 
            DateTime? from = null, DateTime? to = null, int? tableNumber = null,
            string? search = null, string? paymentMode = null, string? orderType = null, int? categoryId = null);
        Task<Order?> GetOrderAsync(int id);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int orderId);

        Task VoidOrderAsync(int orderId, string reason, string authorizedUser);
        Task RefundOrderAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason);
        Task ProcessPartialPaymentAsync(int orderId, decimal cash, decimal card, decimal upi);
    }

    public record OrderItemRefundDto(int ItemId, int QuantityToRefund);
}
