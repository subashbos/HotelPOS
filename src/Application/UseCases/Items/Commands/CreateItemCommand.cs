using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelPOS.Application.UseCases.Items.Commands
{
    public record CreateItemCommand(
        string Name,
        decimal Price,
        decimal TaxPercentage,
        int? CategoryId,
        string? HsnCode,
        string? Barcode,
        int StockQuantity,
        bool TrackInventory
    ) : IRequest<Item>;

    public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Item>
    {
        private readonly IItemRepository _itemRepository;

        public CreateItemCommandHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<Item> Handle(CreateItemCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Item name cannot be empty or whitespace.", nameof(request));

            if (request.Name.Length > 200)
                throw new ArgumentException("Item name must not exceed 200 characters.", nameof(request));

            if (request.Price <= 0)
                throw new ArgumentException("Item price must be greater than zero.", nameof(request));

            if (request.TaxPercentage < 0)
                throw new ArgumentException("Tax percentage cannot be negative.", nameof(request));

            var existing = await _itemRepository.GetAllAsync() ?? new List<Item>();
            if (existing.Any(i => i.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{request.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(request.Barcode) && existing.Any(i => i.Barcode == request.Barcode))
                throw new InvalidOperationException($"Barcode '{request.Barcode}' is already assigned to another item.");

            var item = new Item
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                TaxPercentage = request.TaxPercentage,
                CategoryId = request.CategoryId,
                HsnCode = request.HsnCode,
                Barcode = request.Barcode,
                StockQuantity = request.StockQuantity,
                TrackInventory = request.TrackInventory
            };

            await _itemRepository.AddAsync(item);
            return item;
        }
    }
}
