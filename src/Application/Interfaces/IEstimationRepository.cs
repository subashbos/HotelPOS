using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEstimationRepository
    {
        Task<List<Estimation>> GetEstimationsAsync();
        Task<Estimation?> GetByIdAsync(int id);
        Task AddAsync(Estimation estimation);
        Task UpdateAsync(Estimation estimation);
        Task DeleteAsync(int id);
    }
}
