using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Persistence
{
    public class OrderRepository : IOrderRepository
    {
        private readonly HotelDbContext _context;

        public OrderRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order.Id;
        }

        public async Task<string> GetNextInvoiceNumberAsync(string fiscalYear)
        {
            // Find the highest sequence number for this fiscal year
            // Invoice format: INV/2026-27/0001
            var lastOrder = await _context.Orders
                .Where(o => o.FiscalYear == fiscalYear)
                .OrderByDescending(o => o.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNum = 1;
            if (lastOrder != null && !string.IsNullOrEmpty(lastOrder.InvoiceNumber))
            {
                var parts = lastOrder.InvoiceNumber.Split('/');
                if (parts.Length == 3 && int.TryParse(parts[2], out var lastNum))
                {
                    nextNum = lastNum + 1;
                }
            }

            return $"INV/{fiscalYear}/{nextNum:D4}";
        }

        /// <summary>
        /// Returns all orders with their OrderItems eager-loaded, newest first.
        /// Used exclusively for dashboard report aggregation.
        /// </summary>
        public async Task<List<Order>> GetAllWithItemsAsync()
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Order> Items, int TotalCount)> GetPagedWithItemsAsync(int pageNumber, int pageSize, DateTime? from = null, DateTime? to = null, int? tableNumber = null)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => !o.IsDeleted);

            if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
            if (tableNumber.HasValue) query = query.Where(o => o.TableNumber == tableNumber.Value);

            var total = await query.CountAsync();

            // Support 'All' (-1) by skipping pagination
            if (pageSize > 0)
            {
                query = query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);
            }
            else
            {
                query = query.OrderByDescending(o => o.CreatedAt);
            }

            var items = await query.ToListAsync();
            return (items, total);
        }
    
        public async Task UpdateAsync(Order order)
        {
            var existing = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id);
    
            if (existing == null) throw new KeyNotFoundException($"Order #{order.Id} not found.");
    
            // Update main order properties
            existing.TableNumber = order.TableNumber;
            existing.Subtotal = order.Subtotal;
            existing.GstAmount = order.GstAmount;
            existing.DiscountAmount = order.DiscountAmount;
            existing.TotalAmount = order.TotalAmount;
            existing.PaymentMode = order.PaymentMode;
            existing.UpdatedAt = DateTime.UtcNow;
    
            // Replace items (simpler than syncing individual rows)
            _context.OrderItems.RemoveRange(existing.Items);
            existing.Items = order.Items;
    
            await _context.SaveChangesAsync();
        }
        public async Task<Order?> GetByIdWithItemsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        }

        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.IsDeleted = true;
                order.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
