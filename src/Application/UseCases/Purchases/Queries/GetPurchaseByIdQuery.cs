using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Purchases.Queries
{
    public record GetPurchaseByIdQuery(int Id) : IRequest<Purchase?>;

    public class GetPurchaseByIdQueryHandler : IRequestHandler<GetPurchaseByIdQuery, Purchase?>
    {
        private readonly IPurchaseRepository _repository;

        public GetPurchaseByIdQueryHandler(IPurchaseRepository repository)
        {
            _repository = repository;
        }

        public async Task<Purchase?> Handle(GetPurchaseByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
