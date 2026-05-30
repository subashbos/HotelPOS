namespace HotelPOS.Domain.Interfaces
{
    public interface ISettingRepository
    {
        Task<SystemSetting?> GetByIdAsync(int id);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }
}
