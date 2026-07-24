using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Commands;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Queries;
using FluentValidation;
using HotelPOS.Application.Common.Validators;

namespace HotelPOS.Application.UseCases
{
    public class UnitOfMeasurementService : IUnitOfMeasurementService
    {
        private readonly IMediator? _mediator;
        private readonly IUnitOfMeasurementRepository? _repo;
        private readonly IItemRepository? _itemRepo;
        private readonly IValidator<UnitOfMeasurement> _validator;

        public UnitOfMeasurementService(IMediator mediator)
        {
            _mediator = mediator;
            _validator = new UnitOfMeasurementValidator();
        }

        public UnitOfMeasurementService(IUnitOfMeasurementRepository repo, IItemRepository itemRepo, IValidator<UnitOfMeasurement>? validator = null)
        {
            _repo = repo;
            _itemRepo = itemRepo;
            _validator = validator ?? new UnitOfMeasurementValidator();
        }

        public async Task<List<UnitOfMeasurement>> GetUnitsAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetUnitOfMeasurementsQuery());

            return await _repo!.GetAllAsync() ?? new List<UnitOfMeasurement>();
        }

        public async Task<int> AddUnitAsync(string name, int displayOrder = 0)
        {
            if (_mediator != null)
                return await _mediator.Send(new CreateUnitOfMeasurementCommand(name, displayOrder));

            var trimmedName = name?.Trim() ?? string.Empty;
            var unit = new UnitOfMeasurement { Name = trimmedName, DisplayOrder = displayOrder };
            var result = _validator.Validate(unit);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            var existing = await _repo!.GetAllAsync() ?? new List<UnitOfMeasurement>();
            if (existing.Any(u => u.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Unit '{name}' already exists.");

            var addedUnit = await _repo.AddAsync(unit);
            return addedUnit.Id;
        }

        public async Task UpdateUnitAsync(int id, string name, int displayOrder = 0)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new UpdateUnitOfMeasurementCommand(id, name, displayOrder));
                return;
            }

            if (id <= 0) throw new ArgumentException("Invalid ID");

            var trimmedName = name?.Trim() ?? string.Empty;
            var unit = new UnitOfMeasurement { Id = id, Name = trimmedName, DisplayOrder = displayOrder };
            var result = _validator.Validate(unit);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            var all = await _repo!.GetAllAsync() ?? new List<UnitOfMeasurement>();
            if (all.Any(u => u.Id != id && u.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Unit '{name}' already exists.");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Unit #{id} not found.");

            existing.Name = trimmedName;
            existing.DisplayOrder = displayOrder;
            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteUnitAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteUnitOfMeasurementCommand(id));
                return;
            }

            var items = await _itemRepo!.GetAllAsync() ?? new List<Item>();
            if (items.Any(i => i.UnitId == id))
                throw new InvalidOperationException("Cannot delete unit because it is used by existing menu items. Please reassign the items first.");

            await _repo!.DeleteAsync(id);
        }
    }
}
