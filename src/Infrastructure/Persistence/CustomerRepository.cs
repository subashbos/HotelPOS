using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly HotelDbContext _context;

        public CustomerRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Customer>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.Customers.AsQueryable();
            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetByPhoneAsync(string phone)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        }

        public async Task AddAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                customer.IsActive = false;
                customer.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, int excludeId = 0)
        {
            return await _context.Customers.AnyAsync(c => c.Phone == phone && c.Id != excludeId);
        }
    }
}
