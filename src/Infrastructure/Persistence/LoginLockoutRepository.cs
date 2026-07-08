using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Infrastructure.Persistence
{
    public class LoginLockoutRepository : ILoginLockoutRepository
    {
        private readonly HotelDbContext _context;

        public LoginLockoutRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<LoginLockout?> GetAsync(string normalizedUsername)
        {
            return await _context.LoginLockouts.FindAsync(normalizedUsername);
        }

        public async Task SaveAsync(LoginLockout lockout)
        {
            var existing = await _context.LoginLockouts.FindAsync(lockout.NormalizedUsername);
            if (existing == null)
            {
                _context.LoginLockouts.Add(lockout);
            }
            else
            {
                existing.FailedAttempts = lockout.FailedAttempts;
                existing.LockedUntilUtc = lockout.LockedUntilUtc;
                existing.LastAttemptUtc = lockout.LastAttemptUtc;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ClearAsync(string normalizedUsername)
        {
            var existing = await _context.LoginLockouts.FindAsync(normalizedUsername);
            if (existing != null)
            {
                _context.LoginLockouts.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }
    }
}
