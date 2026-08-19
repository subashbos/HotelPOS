using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Queries
{
    public record GetAllEstimationsQuery() : IRequest<List<Estimation>>;

    public class GetAllEstimationsQueryHandler : IRequestHandler<GetAllEstimationsQuery, List<Estimation>>
    {
        private readonly IEstimationRepository _repository;

        public GetAllEstimationsQueryHandler(IEstimationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Estimation>> Handle(GetAllEstimationsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetEstimationsAsync();
        }
    }
}
