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
