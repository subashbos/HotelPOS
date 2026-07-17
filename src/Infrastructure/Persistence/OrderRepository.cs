using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
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

        /// <summary>
        /// Generates the next invoice identifier for the specified fiscal year using the highest existing invoice number as a base.
        /// </summary>
        /// <param name="fiscalYear">Fiscal year segment to include in the invoice (for example, "2026-27").</param>
        /// <returns>The next invoice number in the format "INV/{fiscalYear}/{sequence:D4}", where {sequence} is a zero-padded 4-digit sequence.</returns>
        /// <exception cref="InvalidOperationException">Thrown when running against SQL Server without an active transaction, or when acquiring the SQL Server application lock for invoice generation fails.</exception>
        public async Task<string> GetNextInvoiceNumberAsync(string fiscalYear)
        {
            if (_context.Database.IsSqlServer())
            {
                if (_context.Database.CurrentTransaction == null)
                {
                    throw new InvalidOperationException("GetNextInvoiceNumberAsync must be called within an active transaction for concurrency safety.");
                }

                var resourceParam = new Microsoft.Data.SqlClient.SqlParameter("@Resource", $"InvoiceGen_{fiscalYear}");
                var modeParam = new Microsoft.Data.SqlClient.SqlParameter("@LockMode", "Exclusive");
                var ownerParam = new Microsoft.Data.SqlClient.SqlParameter("@LockOwner", "Transaction");
                var timeoutParam = new Microsoft.Data.SqlClient.SqlParameter("@LockTimeout", 15000); // 15 seconds

                var resultParam = new Microsoft.Data.SqlClient.SqlParameter
                {
                    ParameterName = "@Result",
                    SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Output
                };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC @Result = sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout",
                    resultParam, resourceParam, modeParam, ownerParam, timeoutParam);

                var lockResult = (int)resultParam.Value;
                if (lockResult < 0)
                {
                    throw new InvalidOperationException($"Could not acquire lock for invoice generation. Result code: {lockResult}");
                }
            }

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

        public async Task<(List<Order> Items, int TotalCount)> GetPagedWithItemsAsync(int pageNumber, int pageSize,
            OrderQueryFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => !o.IsDeleted);

            query = ApplyOrderFilters(query, filter ?? new OrderQueryFilter());

            var total = await query.CountAsync(cancellationToken);

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

            var items = await query.ToListAsync(cancellationToken);
            return (items, total);
        }

        private IQueryable<Order> ApplyOrderFilters(IQueryable<Order> query, OrderQueryFilter filter)
        {
            if (filter.From.HasValue) query = query.Where(o => o.CreatedAt >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(o => o.CreatedAt <= filter.To.Value);
            if (filter.TableNumber.HasValue) query = query.Where(o => o.TableNumber == filter.TableNumber.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search;
                query = query.Where(o => (o.InvoiceNumber != null && o.InvoiceNumber.Contains(search)) ||
                                         (o.CustomerName != null && o.CustomerName.Contains(search)) ||
                                         (o.CustomerPhone != null && o.CustomerPhone.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(filter.PaymentMode) && filter.PaymentMode != "All")
            {
                query = query.Where(o => o.PaymentMode == filter.PaymentMode);
            }

            if (!string.IsNullOrWhiteSpace(filter.OrderType) && filter.OrderType != "All")
            {
                query = query.Where(o => o.OrderType == filter.OrderType);
            }

            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
            {
                var categoryId = filter.CategoryId.Value;
                query = query.Where(o => o.Items.Any(i => _context.Items.Any(item => item.Id == i.ItemId && item.CategoryId == categoryId)));
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == filter.CustomerId.Value);
            }

            return query;
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
            existing.CgstAmount = order.CgstAmount;
            existing.SgstAmount = order.SgstAmount;
            existing.IgstAmount = order.IgstAmount;
            existing.DiscountAmount = order.DiscountAmount;
            existing.TotalAmount = order.TotalAmount;
            existing.PaymentMode = order.PaymentMode;
            existing.OrderType = order.OrderType;
            existing.UpdatedAt = DateTime.UtcNow;

            // Customer fields — previously silently dropped on update
            existing.CustomerName = order.CustomerName;
            existing.CustomerPhone = order.CustomerPhone;
            existing.CustomerGstin = order.CustomerGstin;

            // Replace items (simpler than syncing individual rows)
            _context.OrderItems.RemoveRange(existing.Items);
            existing.Items = order.Items;

            existing.Version++;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    $"Order #{order.Id} was modified by another user. Please reload the order and try again.");
            }
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
 
        public async Task BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();
        public async Task CommitTransactionAsync() => await _context.Database.CommitTransactionAsync();
        public async Task RollbackTransactionAsync() => await _context.Database.RollbackTransactionAsync();
    }
}
