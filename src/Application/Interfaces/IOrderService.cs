using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<int> SaveOrderAsync(SaveOrderRequest request);
        Task<int> SaveOrderInternalAsync(SaveOrderRequest request);
        Task<List<Order>> GetAllOrdersWithItemsAsync();
        Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(PagedOrdersRequest request, CancellationToken cancellationToken = default);
        Task<Order?> GetOrderAsync(int id);
        Task UpdateOrderAsync(Order order);
        Task UpdateOrderInternalAsync(Order order);
        Task DeleteOrderAsync(int orderId);
        Task DeleteOrderInternalAsync(int orderId);

        Task VoidOrderAsync(int orderId, string reason, string authorizedUser);
        Task VoidOrderInternalAsync(int orderId, string reason, string authorizedUser);
        Task RefundOrderAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason);
        Task RefundOrderInternalAsync(int orderId, List<OrderItemRefundDto> itemsToRefund, string reason);
        Task ProcessPartialPaymentAsync(int orderId, decimal cash, decimal card, decimal upi);
        Task ProcessPartialPaymentInternalAsync(int orderId, decimal cash, decimal card, decimal upi);
    }

    public record OrderItemRefundDto(int ItemId, int QuantityToRefund);

    public record SaveOrderRequest(
        List<OrderItem> Items,
        int TableNumber,
        decimal Discount = 0,
        string PaymentMode = PaymentModes.Cash,
        string? CustomerName = null,
        string? CustomerPhone = null,
        string? CustomerGstin = null,
        string OrderType = OrderTypes.DineIn
    );

    public record PagedOrdersRequest(
        int PageNumber,
        int PageSize,
        DateTime? From = null,
        DateTime? To = null,
        int? TableNumber = null,
        string? Search = null,
        string? PaymentMode = null,
        string? OrderType = null,
        int? CategoryId = null
    );
}
