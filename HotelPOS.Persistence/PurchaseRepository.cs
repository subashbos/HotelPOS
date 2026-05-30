using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Persistence
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
            int page, int pageSize, System.DateTime? from, System.DateTime? to, 
            int? supplierId, string? itemName, string? paymentType, string? invoiceNo)
        {
            var query = _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseItems)
                .AsQueryable();

            if (from.HasValue)
                query = query.Where(p => p.PurchaseDate >= from.Value);
            
            if (to.HasValue)
                query = query.Where(p => p.PurchaseDate < to.Value);

            if (supplierId.HasValue && supplierId.Value > 0)
                query = query.Where(p => p.SupplierId == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(paymentType) && paymentType != "All")
                query = query.Where(p => p.PaymentType == paymentType);

            if (!string.IsNullOrWhiteSpace(invoiceNo))
                query = query.Where(p => p.InvoiceNumber.Contains(invoiceNo));

            if (!string.IsNullOrWhiteSpace(itemName))
                query = query.Where(p => p.PurchaseItems.Any(i => i.ItemName.Contains(itemName)));

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

        public async Task AddAsync(Purchase purchase)
        {
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
        }
    }
}
