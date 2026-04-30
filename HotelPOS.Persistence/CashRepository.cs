using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelPOS.Persistence
{
    public class CashRepository : ICashRepository
    {
        private readonly HotelDbContext _context;

        public CashRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<CashSession?> GetCurrentSessionAsync()
        {
            return await _context.CashSessions
                .Where(s => s.Status == "Open")
                .OrderByDescending(s => s.OpenedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(CashSession session)
        {
            _context.CashSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(CashSession session)
        {
            _context.CashSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CashSession>> GetHistoryAsync(int count)
        {
            return await _context.CashSessions
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
