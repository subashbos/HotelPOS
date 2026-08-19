using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Commands
{
    public record CreateUnitOfMeasurementCommand(string Name, int DisplayOrder = 0) : IRequest<int>;

    public class CreateUnitOfMeasurementCommandHandler : IRequestHandler<CreateUnitOfMeasurementCommand, int>
    {
        private readonly IUnitOfMeasurementRepository _repo;
        private readonly IAuthorizationService _authorization;

        public CreateUnitOfMeasurementCommandHandler(IUnitOfMeasurementRepository repo, IAuthorizationService authorization)
        {
            _repo = repo;
            _authorization = authorization;
        }

        public async Task<int> Handle(CreateUnitOfMeasurementCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsureEditPermission(PermissionModules.Units);

            var existing = await _repo.GetAllAsync() ?? new List<UnitOfMeasurement>();
            if (existing.Any(u => u.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Unit '{request.Name}' already exists.");

            var unit = new UnitOfMeasurement { Name = request.Name.Trim(), DisplayOrder = request.DisplayOrder };
            var result = await _repo.AddAsync(unit);
            return result.Id;
        }
    }
}
