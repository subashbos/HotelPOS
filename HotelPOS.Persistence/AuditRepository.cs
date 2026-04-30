using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Persistence
{
    public class AuditRepository : IAuditRepository
    {
        private readonly HotelDbContext _context;

        public AuditRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog log)
        {
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetLogsAsync(DateTime? from, DateTime? to)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (from.HasValue) query = query.Where(l => l.Timestamp >= from.Value.ToUniversalTime());
            if (to.HasValue) query = query.Where(l => l.Timestamp <= to.Value.ToUniversalTime());

            return await query.OrderByDescending(l => l.Timestamp).ToListAsync();
        }
    }
}
