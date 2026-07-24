using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public record UpdateUnitOfMeasurementCommand(int Id, string Name, int DisplayOrder = 0) : IRequest;

    public class UpdateUnitOfMeasurementCommandHandler : IRequestHandler<UpdateUnitOfMeasurementCommand>
    {
        private readonly IUnitOfMeasurementRepository _repo;

        public UpdateUnitOfMeasurementCommandHandler(IUnitOfMeasurementRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(UpdateUnitOfMeasurementCommand request, CancellationToken cancellationToken)
        {
            var all = await _repo.GetAllAsync() ?? new List<UnitOfMeasurement>();
            if (all.Any(u => u.Id != request.Id && u.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Unit '{request.Name}' already exists.");

            var existing = await _repo.GetByIdAsync(request.Id);
            if (existing == null) throw new KeyNotFoundException($"Unit #{request.Id} not found.");

            existing.Name = request.Name.Trim();
            existing.DisplayOrder = request.DisplayOrder;
            await _repo.UpdateAsync(existing);
        }
    }
}
