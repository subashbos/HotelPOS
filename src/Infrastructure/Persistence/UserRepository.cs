#nullable enable

using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class UserRepository : IUserRepository
    {
        private readonly HotelDbContext _context;

        public UserRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var query = _context.Users
                .Include(u => u.RoleDetails)
                .ThenInclude(r => r!.Permissions);

            // EF.Functions.Collate keeps this sargable (unlike wrapping the column in ToLower(),
            // which forces a table scan) while being explicitly case-insensitive regardless of the
            // database's default collation. SQL Server and SQLite (used by the test suite) don't
            // share collation names, so pick the one each provider actually understands.
            return _context.Database.IsSqlServer()
                ? await query.FirstOrDefaultAsync(u => EF.Functions.Collate(u.Username, "SQL_Latin1_General_CP1_CI_AS") == username)
                : await query.FirstOrDefaultAsync(u => EF.Functions.Collate(u.Username, "NOCASE") == username);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.OrderBy(u => u.Username).ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task AddAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // Soft-delete: preserve the record for audit trail & FK integrity
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }
        }
    }
}
