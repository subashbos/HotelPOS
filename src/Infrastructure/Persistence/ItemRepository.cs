using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class ItemRepository : IItemRepository
    {
        private readonly HotelDbContext _context;

        public ItemRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddAsync(Item item)
        {
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item.Id;
        }

        public async Task AddRangeAsync(List<Item> items)
        {
            _context.Items.AddRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Item>> GetAllAsync()
        {
            return await _context.Items.AsNoTracking().Include(i => i.Category).Include(i => i.Unit).ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items.Include(i => i.Category).Include(i => i.Unit).FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Item>> GetByIdsAsync(List<int> ids)
        {
            return await _context.Items.Where(i => ids.Contains(i.Id)).ToListAsync();
        }

        public async Task UpdateAsync(Item item)
        {
            _context.Items.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> TryDeductStockAsync(int itemId, int quantity)
        {
            var rows = await _context.Items
                .Where(i => i.Id == itemId && (quantity <= 0 || i.StockQuantity >= quantity))
                .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.StockQuantity, i => i.StockQuantity - quantity));
            return rows > 0;
        }

        public async Task UpdateRangeAsync(List<Item> items)
        {
            _context.Items.UpdateRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item is not null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
