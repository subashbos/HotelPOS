using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using FluentValidation;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Items.Queries;
using AutoMapper;

using MediatR;


namespace HotelPOS.Application.UseCases
{
    public class ItemService : IItemService
    {
        private readonly IMediator? _mediator;
        private readonly IItemRepository _itemRepository;
        private readonly IValidator<CreateItemCommand> _validator;
        private readonly IMapper _mapper;

        public ItemService(IItemRepository itemRepository, IMediator? mediator = null, IValidator<CreateItemCommand>? validator = null, IMapper? mapper = null)
        {
            _itemRepository = itemRepository;
            _mediator = mediator;
            _validator = validator ?? new CreateItemCommandValidator();
            
            if (mapper == null)
            {
                var cfg = new AutoMapper.MapperConfiguration(
                    expr => expr.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
                    Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                _mapper = cfg.CreateMapper();
            }
            else
            {
                _mapper = mapper;
            }
        }

        public async Task<int> AddItemAsync(CreateItemDto dto)
        {
            if (_mediator != null)
            {
                var command = new CreateItemCommand(
                    dto.Name, dto.Price, dto.TaxPercentage, dto.CategoryId,
                    dto.HsnCode, dto.Barcode, dto.StockQuantity, dto.TrackInventory
                );
                var item = await _mediator.Send(command);
                return item.Id;
            }

            ValidateDto(dto);

            var existing = await _itemRepository!.GetAllAsync() ?? new List<Item>();
            if (existing.Any(i => i.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{dto.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && existing.Any(i => i.Barcode == dto.Barcode))
                throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already assigned to another item.");

            var itemEntity = _mapper!.Map<Item>(dto);

            return await _itemRepository.AddAsync(itemEntity);
        }

        private void ValidateDto(CreateItemDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var command = _mapper!.Map<CreateItemCommand>(dto);

            var result = _validator!.Validate(command);
            if (!result.IsValid)
            {
                var firstError = result.Errors[0];
                throw new ArgumentException(firstError.ErrorMessage, nameof(dto));
            }
        }

        public async Task<List<Item>> GetItemsAsync()
        {
            if (_mediator != null)
            {
                return await _mediator.Send(new GetItemsQuery());
            }

            return await _itemRepository!.GetAllAsync() ?? new List<Item>();
        }

        public async Task<List<Item>> GetItemsByIdsAsync(List<int> ids)
        {
            if (ids == null || ids.Count == 0) return new List<Item>();
            return await _itemRepository.GetByIdsAsync(ids) ?? new List<Item>();
        }

        public async Task UpdateItemAsync(int id, CreateItemDto dto)
        {
            if (_mediator != null)
            {
                var command = new UpdateItemCommand(
                    id, dto.Name, dto.Price, dto.TaxPercentage, dto.CategoryId,
                    dto.HsnCode, dto.Barcode, dto.StockQuantity, dto.TrackInventory
                );
                await _mediator.Send(command);
                return;
            }

            ValidateDto(dto);

            var existingAll = await _itemRepository!.GetAllAsync() ?? new List<Item>();
            if (existingAll.Any(i => i.Id != id && i.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{dto.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && existingAll.Any(i => i.Id != id && i.Barcode == dto.Barcode))
                throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already assigned to another item.");

            var item = await _itemRepository.GetByIdAsync(id);
            if (item == null) throw new KeyNotFoundException("Item not found");

            _mapper!.Map(dto, item);

            await _itemRepository.UpdateAsync(item);
        }

        public async Task DeductStockAsync(int itemId, int quantity)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null || !item.TrackInventory) return;

            // Deduction itself is a single atomic guarded UPDATE (see TryDeductStockAsync) so two
            // concurrent orders for the last unit can't both pass a stale in-memory stock check.
            var deducted = await _itemRepository.TryDeductStockAsync(itemId, quantity);
            if (!deducted && quantity > 0)
            {
                var current = await _itemRepository.GetByIdAsync(itemId);
                throw new InvalidOperationException($"Insufficient stock for item: {item.Name}. Required: {quantity}, Available: {current?.StockQuantity ?? 0}");
            }
        }

        public async Task DeleteItemAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid item ID.", nameof(id));

            if (_mediator != null)
            {
                await _mediator.Send(new DeleteItemCommand(id));
                return;
            }

            await _itemRepository.DeleteAsync(id);
        }

        public async Task<(int Added, int Skipped)> BulkAddAsync(List<CreateItemDto> items)
        {
            int skipped = 0;
            var existing = await _itemRepository.GetAllAsync() ?? new List<Item>();
            var existingNames = new HashSet<string>(
                existing.Select(i => i.Name.Trim().ToLowerInvariant()));
            var existingBarcodes = new HashSet<string>(
                existing.Where(i => !string.IsNullOrWhiteSpace(i.Barcode))
                        .Select(i => i.Barcode!));

            var toAdd = new List<Item>();

            foreach (var dto in items)
            {
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
                { skipped++; continue; }

                if (existingNames.Contains(dto.Name.Trim().ToLowerInvariant()))
                { skipped++; continue; }

                if (!string.IsNullOrWhiteSpace(dto.Barcode) && existingBarcodes.Contains(dto.Barcode))
                { skipped++; continue; }

                toAdd.Add(new Item
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
            }

            if (toAdd.Count > 0)
            {
                await _itemRepository.AddRangeAsync(toAdd);
            }

            return (toAdd.Count, skipped);
        }
    }
}
