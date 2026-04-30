using HotelPOS.Domain;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Persistence
{
    public class SettingRepository : ISettingRepository
    {
        private readonly HotelDbContext _context;

        public SettingRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<SystemSetting?> GetByIdAsync(int id)
        {
            return await _context.SystemSettings.FindAsync(id);
        }

        public async Task UpdateAsync(SystemSetting setting)
        {
            _context.SystemSettings.Update(setting);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(SystemSetting setting)
        {
            _context.SystemSettings.Add(setting);
            await _context.SaveChangesAsync();
        }
    }
}
