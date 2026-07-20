using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.CashSessions.Queries
{
    public record GetCurrentSessionQuery() : IRequest<CashSession?>;

    public class GetCurrentSessionQueryHandler : IRequestHandler<GetCurrentSessionQuery, CashSession?>
    {
        private readonly ICashRepository _repository;

        public GetCurrentSessionQueryHandler(ICashRepository repository)
        {
            _repository = repository;
        }

        public async Task<CashSession?> Handle(GetCurrentSessionQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetCurrentSessionAsync();
        }
    }
}
