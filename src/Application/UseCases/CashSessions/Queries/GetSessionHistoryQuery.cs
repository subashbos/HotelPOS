using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.CashSessions.Queries
{
    public record GetSessionHistoryQuery(int Count = 30) : IRequest<List<CashSession>>;

    public class GetSessionHistoryQueryHandler : IRequestHandler<GetSessionHistoryQuery, List<CashSession>>
    {
        private readonly ICashRepository _repository;

        public GetSessionHistoryQueryHandler(ICashRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CashSession>> Handle(GetSessionHistoryQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetHistoryAsync(request.Count);
        }
    }
}
