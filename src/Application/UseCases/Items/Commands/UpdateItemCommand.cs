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
    public record UpdateItemCommand(
        int Id,
        string Name,
        decimal Price,
        decimal TaxPercentage,
        int? CategoryId,
        string? HsnCode,
        string? Barcode,
        int StockQuantity,
        bool TrackInventory,
        int UnitId
    ) : IRequest<Item>;

    public class UpdateItemCommandHandler : IRequestHandler<UpdateItemCommand, Item>
    {
        private readonly IItemRepository _itemRepository;

        public UpdateItemCommandHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<Item> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Invalid item ID.", nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Item name cannot be empty or whitespace.", nameof(request));

            if (request.Name.Length > 200)
                throw new ArgumentException("Item name must not exceed 200 characters.", nameof(request));

            if (request.Price <= 0)
                throw new ArgumentException("Item price must be greater than zero.", nameof(request));

            if (request.TaxPercentage < 0)
                throw new ArgumentException("Tax percentage cannot be negative.", nameof(request));

            var existingAll = await _itemRepository.GetAllAsync() ?? new List<Item>();
            if (existingAll.Any(i => i.Id != request.Id && i.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"An item with the name '{request.Name}' already exists.");

            if (!string.IsNullOrWhiteSpace(request.Barcode) && existingAll.Any(i => i.Id != request.Id && i.Barcode == request.Barcode))
                throw new InvalidOperationException($"Barcode '{request.Barcode}' is already assigned to another item.");

            var item = await _itemRepository.GetByIdAsync(request.Id);
            if (item == null)
                throw new KeyNotFoundException($"Item #{request.Id} not found.");

            item.Name = request.Name.Trim();
            item.Price = request.Price;
            item.TaxPercentage = request.TaxPercentage;
            item.CategoryId = request.CategoryId;
            item.HsnCode = request.HsnCode;
            item.Barcode = request.Barcode;
            item.StockQuantity = request.StockQuantity;
            item.TrackInventory = request.TrackInventory;
            item.UnitId = request.UnitId;

            await _itemRepository.UpdateAsync(item);
            return item;
        }
    }
}
