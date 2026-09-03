using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Queries
{
    public record GetPagedPurchasesQuery(int Page, int PageSize, PurchaseQueryFilter? Filter = null)
        : IRequest<(List<Purchase> purchases, int totalCount)>;

    public class GetPagedPurchasesQueryHandler : IRequestHandler<GetPagedPurchasesQuery, (List<Purchase> purchases, int totalCount)>
    {
        private readonly IPurchaseRepository _repository;

        public GetPagedPurchasesQueryHandler(IPurchaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<(List<Purchase> purchases, int totalCount)> Handle(GetPagedPurchasesQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetPagedPurchasesAsync(request.Page, request.PageSize, request.Filter);
        }
    }
}
