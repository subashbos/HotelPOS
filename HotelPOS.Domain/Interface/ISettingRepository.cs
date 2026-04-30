namespace HotelPOS.Domain.Interface
{
    public interface ISettingRepository
    {
        Task<SystemSetting?> GetByIdAsync(int id);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }
}
