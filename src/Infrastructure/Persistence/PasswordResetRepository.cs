using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly HotelDbContext _context;

        public PasswordResetRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PasswordResetRequest request)
        {
            _context.PasswordResetRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        // Left tracked: PasswordResetService mutates the returned request in place and persists
        // via UpdateAsync's blind _context.PasswordResetRequests.Update(), which throws if this
        // fetch is untracked but the same row is already tracked elsewhere in the DbContext.
        public async Task<PasswordResetRequest?> GetLatestActiveAsync(int userId)
        {
            return await _context.PasswordResetRequests
                .Where(r => r.UserId == userId && !r.Used)
                .OrderByDescending(r => r.CreatedUtc)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(PasswordResetRequest request)
        {
            _context.PasswordResetRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }
}
