using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface IItemService
    {
        Task<int> AddItemAsync(CreateItemDto dto);
        Task<List<Item>> GetItemsAsync();
        Task UpdateItemAsync(int id, CreateItemDto dto);
        Task DeleteItemAsync(int id);
        Task DeductStockAsync(int itemId, int quantity);
        Task<(int Added, int Skipped)> BulkAddAsync(List<CreateItemDto> items);
    }
}
