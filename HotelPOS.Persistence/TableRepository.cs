using HotelPOS.Domain;
using HotelPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Persistence
{
    public class TableRepository : ITableRepository
    {
        private readonly HotelDbContext _context;

        public TableRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<int> AddAsync(Table table)
        {
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
            return table.Id;
        }

        public async Task<List<Table>> GetAllAsync()
        {
            return await _context.Tables.AsNoTracking()
                .Where(t => !t.IsDeleted)
                .ToListAsync();
        }

        public async Task<Table?> GetByIdAsync(int id)
        {
            return await _context.Tables.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        public async Task UpdateAsync(Table table)
        {
            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table is not null)
            {
                table.IsDeleted = true;
                _context.Tables.Update(table);
                await _context.SaveChangesAsync();
            }
        }
    }
}
