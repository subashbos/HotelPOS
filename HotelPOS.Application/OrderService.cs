using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IMediator _mediator;
        private readonly IItemService _itemService;

        public OrderService(IOrderRepository repo, IMediator mediator, IItemService itemService)
        {
            _repo = repo;
            _mediator = mediator;
            _itemService = itemService;
        }

        public async Task<int> SaveOrderAsync(List<OrderItem> items, int tableNumber, decimal discount = 0, string paymentMode = "Cash", string? customerName = null, string? customerPhone = null, string? customerGstin = null)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.", nameof(items));

            var orderItems = items
                .Select(x => new OrderItem
                {
                    ItemId = x.ItemId,
                    ItemName = x.ItemName,
                    Quantity = x.Quantity,
                    Price = x.Price,
                    TaxPercentage = x.TaxPercentage,
                    Total = x.Total
                })
                .ToList();

            var now = DateTime.UtcNow;
            var fy = GetFiscalYear(now.ToLocalTime());
            var inv = await _repo.GetNextInvoiceNumberAsync(fy);

            var subtotal = orderItems.Sum(x => x.Total);
            var gst = Math.Round(orderItems.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);
            
            // Assume Intrastate default for Hotel POS (CGST = 50%, SGST = 50%)
            var cgst = Math.Round(gst / 2m, 2);
            var sgst = gst - cgst; // to avoid rounding mismatch
            var igst = 0m;

            var total = Math.Max(0, subtotal + gst - discount);

            var order = new Order
            {
                InvoiceNumber = inv,
                FiscalYear = fy,
                CreatedAt = now,
                TableNumber = tableNumber,
                Items = orderItems,
                Subtotal = subtotal,
                GstAmount = gst,
                CgstAmount = cgst,
                SgstAmount = sgst,
                IgstAmount = igst,
                DiscountAmount = discount,
                TotalAmount = total,
                PaymentMode = paymentMode,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerGstin = customerGstin
            };

            var orderId = await _repo.AddAsync(order);

            // Deduct Stock
            foreach (var item in orderItems)
            {
                await _itemService.DeductStockAsync(item.ItemId, item.Quantity);
            }

            await _mediator.Publish(new EntityActionEvent("Order", orderId, "Create", $"Total: {total:N2}, Table: {tableNumber}"));
            return orderId;
        }

        private string GetFiscalYear(DateTime date)
        {
            // India: April 1 to March 31
            int year = date.Month < 4 ? date.Year - 1 : date.Year;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        public Task<List<Order>> GetAllOrdersWithItemsAsync()
            => _repo.GetAllWithItemsAsync();
    
        public Task<(List<Order> Items, int TotalCount)> GetPagedOrdersAsync(int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null, int? tableNumber = null)
            => _repo.GetPagedWithItemsAsync(pageNumber, pageSize, from, to, tableNumber);

        public async Task UpdateOrderAsync(Order order)
        {
            if (order.Items == null || order.Items.Count == 0)
                throw new ArgumentException("Cannot save an empty order.");
    
            var oldOrder = await _repo.GetByIdWithItemsAsync(order.Id);
            if (oldOrder == null) throw new KeyNotFoundException($"Order #{order.Id} not found.");

            // Stock Reconciliation
            var oldMap = oldOrder.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            var newMap = order.Items.GroupBy(i => i.ItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            // Return all old stock first (or we can diff, but this is safer for complex changes)
            foreach (var kvp in oldMap)
            {
                await _itemService.DeductStockAsync(kvp.Key, -kvp.Value); // Deduct negative = return
            }

            // Deduct all new stock
            foreach (var kvp in newMap)
            {
                await _itemService.DeductStockAsync(kvp.Key, kvp.Value);
            }

            var oldTotal = oldOrder.TotalAmount;

            // Recalculate totals
            order.Subtotal = order.Items.Sum(x => x.Total);
            order.GstAmount = Math.Round(order.Items.Sum(x => x.Price * x.Quantity * (x.TaxPercentage / 100m)), 2);
            
            order.CgstAmount = Math.Round(order.GstAmount / 2m, 2);
            order.SgstAmount = order.GstAmount - order.CgstAmount;
            order.IgstAmount = 0m;

            order.TotalAmount = Math.Max(0, order.Subtotal + order.GstAmount - order.DiscountAmount);
    
            await _repo.UpdateAsync(order);
            await _mediator.Publish(new EntityActionEvent("Order", order.Id, "Update", $"Old Total: {oldTotal:N2} -> New Total: {order.TotalAmount:N2}"));
        }
        public async Task DeleteOrderAsync(int orderId)
        {
            var existing = await _repo.GetByIdWithItemsAsync(orderId);
            if (existing != null)
            {
                foreach (var item in existing.Items)
                {
                    await _itemService.DeductStockAsync(item.ItemId, -item.Quantity);
                }
                
                await _repo.DeleteAsync(orderId);
                await _mediator.Publish(new EntityActionEvent("Order", orderId, "Delete", "Soft Deleted"));
            }
        }
    }
}
