using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly HotelDbContext _context;

        public RefreshTokenRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken token)
        {
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            return await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        public async Task UpdateAsync(RefreshToken token)
        {
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeFamilyAsync(Guid familyId, DateTime revokedUtc)
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.FamilyId == familyId && t.RevokedUtc == null)
                .ToListAsync();

            if (tokens.Count == 0) return;

            foreach (var token in tokens)
            {
                token.RevokedUtc = revokedUtc;
            }

            await _context.SaveChangesAsync();
        }
    }
}
