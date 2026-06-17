using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Queries
{
    public record GetAllPurchasesQuery() : IRequest<List<Purchase>>;

    public class GetAllPurchasesQueryHandler : IRequestHandler<GetAllPurchasesQuery, List<Purchase>>
    {
        private readonly IPurchaseRepository _repository;

        public GetAllPurchasesQueryHandler(IPurchaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Purchase>> Handle(GetAllPurchasesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetPurchasesAsync();
        }
    }
}
