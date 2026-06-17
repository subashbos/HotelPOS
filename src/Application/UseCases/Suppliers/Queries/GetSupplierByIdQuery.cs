using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Suppliers.Queries
{
    public record GetSupplierByIdQuery(int Id) : IRequest<Supplier?>;

    public class GetSupplierByIdQueryHandler : IRequestHandler<GetSupplierByIdQuery, Supplier?>
    {
        private readonly ISupplierRepository _repository;

        public GetSupplierByIdQueryHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<Supplier?> Handle(GetSupplierByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
