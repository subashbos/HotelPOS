using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly HotelDbContext _context;

        public SupplierRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers.ToListAsync();
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<Supplier?> GetByNameAsync(string name)
        {
            return await _context.Suppliers.FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
        }

        public async Task AddAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var supplier = await GetByIdAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByNameAsync(string name, int excludeId = 0)
        {
            return await _context.Suppliers.AnyAsync(s => s.Name.ToLower() == name.ToLower() && s.Id != excludeId);
        }
    }
}
