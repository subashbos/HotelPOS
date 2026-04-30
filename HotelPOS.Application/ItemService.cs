using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Application
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;

        public ItemService(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<int> AddItemAsync(CreateItemDto dto)
        {
            ValidateDto(dto);

            var item = new Item
            {
                Name = dto.Name.Trim(),
                Price = dto.Price,
                TaxPercentage = dto.TaxPercentage,
                HsnCode = dto.HsnCode,
                CategoryId = dto.CategoryId,
                StockQuantity = dto.StockQuantity,
                TrackInventory = dto.TrackInventory,
                Barcode = dto.Barcode
            };

            return await _itemRepository.AddAsync(item);
        }

        private void ValidateDto(CreateItemDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Item name cannot be empty or whitespace.", nameof(dto));

            if (dto.Name.Length > 200)
                throw new ArgumentException("Item name must not exceed 200 characters.", nameof(dto));

            if (dto.Price <= 0)
                throw new ArgumentException("Item price must be greater than zero.", nameof(dto));
        }

        public async Task<List<Item>> GetItemsAsync()
        {
            return await _itemRepository.GetAllAsync();
        }

        public async Task UpdateItemAsync(int id, CreateItemDto dto)
        {
            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null) throw new KeyNotFoundException("Item not found");

            item.Name = dto.Name.Trim();
            item.Price = dto.Price;
            item.TaxPercentage = dto.TaxPercentage;
            item.HsnCode = dto.HsnCode;
            item.CategoryId = dto.CategoryId;
            item.StockQuantity = dto.StockQuantity;
            item.TrackInventory = dto.TrackInventory;
            item.Barcode = dto.Barcode;

            await _itemRepository.UpdateAsync(item);
        }

        public async Task DeductStockAsync(int itemId, int quantity)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item != null && item.TrackInventory)
            {
                item.StockQuantity -= quantity;
                await _itemRepository.UpdateAsync(item);
            }
        }

        public async Task DeleteItemAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid item ID.", nameof(id));

            await _itemRepository.DeleteAsync(id);
        }

        public async Task<(int Added, int Skipped)> BulkAddAsync(List<CreateItemDto> items)
        {
            int added = 0, skipped = 0;
            var existing = await _itemRepository.GetAllAsync();
            var existingNames = new HashSet<string>(
                existing.Select(i => i.Name.Trim().ToLowerInvariant()));

            foreach (var dto in items)
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
                { skipped++; continue; }

                if (existingNames.Contains(dto.Name.Trim().ToLowerInvariant()))
                { skipped++; continue; }

                await _itemRepository.AddAsync(new Item 
                { 
                    Name = dto.Name.Trim(), 
                    Price = dto.Price, 
                    TaxPercentage = dto.TaxPercentage, 
                    CategoryId = dto.CategoryId,
                    Barcode = dto.Barcode
                });
                existingNames.Add(dto.Name.Trim().ToLowerInvariant());
                added++;
            }
            return (added, skipped);
        }
    }
}
