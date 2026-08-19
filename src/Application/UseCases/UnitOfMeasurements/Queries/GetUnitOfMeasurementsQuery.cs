using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.UnitOfMeasurements.Queries
{
    public record GetUnitOfMeasurementsQuery() : IRequest<List<UnitOfMeasurement>>;

    public class GetUnitOfMeasurementsQueryHandler : IRequestHandler<GetUnitOfMeasurementsQuery, List<UnitOfMeasurement>>
    {
        private readonly IUnitOfMeasurementRepository _repo;

        public GetUnitOfMeasurementsQueryHandler(IUnitOfMeasurementRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<UnitOfMeasurement>> Handle(GetUnitOfMeasurementsQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetAllAsync() ?? new List<UnitOfMeasurement>();
        }
    }
}
