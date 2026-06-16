using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
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

            var existing = await _itemRepository.GetAllAsync() ?? new List<Item>();
            if (existing.Any(i => i.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{dto.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && existing.Any(i => i.Barcode == dto.Barcode))
                throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already assigned to another item.");

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

            var validator = new HotelPOS.Application.UseCases.Items.Commands.CreateItemCommandValidator();
            var command = new HotelPOS.Application.UseCases.Items.Commands.CreateItemCommand(
                dto.Name,
                dto.Price,
                dto.TaxPercentage,
                dto.CategoryId,
                dto.HsnCode,
                dto.Barcode,
                dto.StockQuantity,
                dto.TrackInventory
            );

            var result = validator.Validate(command);
            if (!result.IsValid)
            {
                var firstError = result.Errors.First();
                throw new ArgumentException(firstError.ErrorMessage, nameof(dto));
            }
        }

        public async Task<List<Item>> GetItemsAsync()
        {
            return await _itemRepository.GetAllAsync() ?? new List<Item>();
        }

        public async Task UpdateItemAsync(int id, CreateItemDto dto)
        {
            ValidateDto(dto);

            var existingAll = await _itemRepository.GetAllAsync() ?? new List<Item>();
            if (existingAll.Any(i => i.Id != id && i.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{dto.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && existingAll.Any(i => i.Id != id && i.Barcode == dto.Barcode))
                throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already assigned to another item.");

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
                if (quantity > 0 && item.StockQuantity < quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for item: {item.Name}. Required: {quantity}, Available: {item.StockQuantity}");
                }

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
            var existing = await _itemRepository.GetAllAsync() ?? new List<Item>();
            var existingNames = new HashSet<string>(
                existing.Select(i => i.Name.Trim().ToLowerInvariant()));
            var existingBarcodes = new HashSet<string>(
                existing.Where(i => !string.IsNullOrWhiteSpace(i.Barcode))
                        .Select(i => i.Barcode!));

            foreach (var dto in items)
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
                { skipped++; continue; }

                if (existingNames.Contains(dto.Name.Trim().ToLowerInvariant()))
                { skipped++; continue; }

                if (!string.IsNullOrWhiteSpace(dto.Barcode) && existingBarcodes.Contains(dto.Barcode))
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
                if (!string.IsNullOrWhiteSpace(dto.Barcode))
                {
                    existingBarcodes.Add(dto.Barcode);
                }
                added++;
            }
            return (added, skipped);
        }
    }
}
