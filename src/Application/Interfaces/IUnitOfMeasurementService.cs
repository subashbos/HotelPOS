using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IUnitOfMeasurementService
    {
        Task<List<UnitOfMeasurement>> GetUnitsAsync();
        Task<int> AddUnitAsync(string name, int displayOrder = 0);
        Task UpdateUnitAsync(int id, string name, int displayOrder = 0);
        Task DeleteUnitAsync(int id);
    }
}
