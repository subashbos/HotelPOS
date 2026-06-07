using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface ISettingRepository
    {
        Task<SystemSetting?> GetByIdAsync(int id);
        Task UpdateAsync(SystemSetting setting);
        Task AddAsync(SystemSetting setting);
    }
}
