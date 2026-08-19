using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class CashRepository : ICashRepository
    {
        private readonly HotelDbContext _context;

        public CashRepository(HotelDbContext context)
        {
            _context = context;
        }

        // Left tracked: CashService/CloseSessionCommand mutate the returned session in place and
        // persist via UpdateAsync's blind _context.CashSessions.Update(), which throws if this
        // fetch is untracked but the same row is already tracked elsewhere in the DbContext.
        public async Task<CashSession?> GetCurrentSessionAsync()
        {
            return await _context.CashSessions
                .Where(s => s.Status == CashSessionStatuses.Open)
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(CashSession session)
        {
            _context.CashSessions.Add(session);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("A session is already open.");
            }
        }

        public async Task UpdateAsync(CashSession session)
        {
            _context.CashSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CashSession>> GetHistoryAsync(int count)
        {
            return await _context.CashSessions
                .AsNoTracking()
                .OrderByDescending(s => s.OpenedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<decimal> GetSalesTotalAsync(DateTime since)
        {
            return await _context.Orders
                .Where(o => !o.IsDeleted && o.CreatedAt >= since)
                .SumAsync(o => o.TotalAmount);
        }
    }
}
