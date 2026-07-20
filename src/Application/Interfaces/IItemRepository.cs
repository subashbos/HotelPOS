using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface IItemRepository
    {
        Task<List<Item>> GetAllAsync();
        Task<Item?> GetByIdAsync(int id);
        Task<List<Item>> GetByIdsAsync(List<int> ids);
        Task<int> AddAsync(Item item);
        Task AddRangeAsync(List<Item> items);
        Task UpdateAsync(Item item);
        Task UpdateRangeAsync(List<Item> items);
        Task DeleteAsync(int id);
    }
}
