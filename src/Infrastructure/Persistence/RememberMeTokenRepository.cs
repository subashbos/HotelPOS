using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class RememberMeTokenRepository : IRememberMeTokenRepository
    {
        private readonly HotelDbContext _context;

        public RememberMeTokenRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RememberMeToken token)
        {
            _context.RememberMeTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        // Left tracked: RememberMeService mutates the returned token in place and persists via
        // UpdateAsync's blind _context.RememberMeTokens.Update(), which throws if this fetch is
        // untracked but the same row is already tracked elsewhere in the DbContext.
        public async Task<RememberMeToken?> GetByHashAsync(string tokenHash)
        {
            return await _context.RememberMeTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task UpdateAsync(RememberMeToken token)
        {
            _context.RememberMeTokens.Update(token);
            await _context.SaveChangesAsync();
        }
    }
}
