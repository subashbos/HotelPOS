using HotelPOS.Application.DTOs.Item;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IItemService
    {
        Task<int> AddItemAsync(CreateItemDto dto);
        Task<List<Item>> GetItemsAsync();
        Task<List<Item>> GetItemsByIdsAsync(List<int> ids);
        Task UpdateItemAsync(int id, CreateItemDto dto);
        Task DeleteItemAsync(int id);
        Task DeductStockAsync(int itemId, int quantity);
        Task<(int Added, int Skipped)> BulkAddAsync(List<CreateItemDto> items);
    }
}
