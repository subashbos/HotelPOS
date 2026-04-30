using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface ISettingService
    {
        Task<SystemSetting> GetSettingsAsync();
        Task SaveSettingsAsync(SystemSetting settings);
    }
}
