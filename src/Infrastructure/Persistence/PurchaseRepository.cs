using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly HotelDbContext _context;

        public PurchaseRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetSuppliersAsync()
        {
            return await _context.Suppliers.ToListAsync();
        }

        public async Task<List<Purchase>> GetPurchasesAsync()
        {
            return await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                .ToListAsync();
        }

        public async Task<(List<Purchase> purchases, int totalCount)> GetPagedPurchasesAsync(
            int page, int pageSize, PurchaseQueryFilter? filter = null)
        {
            filter ??= new PurchaseQueryFilter();

            var query = _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                .AsQueryable();

            if (filter.From.HasValue)
                query = query.Where(p => p.PurchaseDate >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(p => p.PurchaseDate < filter.To.Value);

            if (filter.SupplierId.HasValue && filter.SupplierId.Value > 0)
                query = query.Where(p => p.SupplierId == filter.SupplierId.Value);

            if (!string.IsNullOrWhiteSpace(filter.PaymentType) && filter.PaymentType != "All")
                query = query.Where(p => p.PaymentType == filter.PaymentType);

            if (!string.IsNullOrWhiteSpace(filter.InvoiceNo))
                query = query.Where(p => p.InvoiceNumber.Contains(filter.InvoiceNo));

            if (!string.IsNullOrWhiteSpace(filter.ItemName))
                query = query.Where(p => p.PurchaseItems.Any(i => i.ItemName.Contains(filter.ItemName)));

            var totalCount = await query.CountAsync();

            if (pageSize > 0)
            {
                query = query.OrderByDescending(p => p.PurchaseDate)
                             .ThenByDescending(p => p.Id)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize);
            }
            else
            {
                query = query.OrderByDescending(p => p.PurchaseDate).ThenByDescending(p => p.Id);
            }

            var purchases = await query.ToListAsync();
            return (purchases, totalCount);
        }

        private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;

        public async Task BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task AddAsync(Purchase purchase)
        {
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
        }
    }
}
