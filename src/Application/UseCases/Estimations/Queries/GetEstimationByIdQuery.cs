using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Estimations.Queries
{
    public record GetEstimationByIdQuery(int Id) : IRequest<Estimation?>;

    public class GetEstimationByIdQueryHandler : IRequestHandler<GetEstimationByIdQuery, Estimation?>
    {
        private readonly IEstimationRepository _repository;

        public GetEstimationByIdQueryHandler(IEstimationRepository repository)
        {
            _repository = repository;
        }

        public async Task<Estimation?> Handle(GetEstimationByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
