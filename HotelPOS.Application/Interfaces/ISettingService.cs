using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ISettingService
    {
        Task<SystemSetting> GetSettingsAsync();
        Task SaveSettingsAsync(SystemSetting settings);
    }
}
