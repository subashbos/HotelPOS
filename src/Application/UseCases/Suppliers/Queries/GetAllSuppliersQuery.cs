using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Suppliers.Queries
{
    public record GetAllSuppliersQuery() : IRequest<List<Supplier>>;

    public class GetAllSuppliersQueryHandler : IRequestHandler<GetAllSuppliersQuery, List<Supplier>>
    {
        private readonly ISupplierRepository _repository;

        public GetAllSuppliersQueryHandler(ISupplierRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Supplier>> Handle(GetAllSuppliersQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetAllAsync() ?? new List<Supplier>();
        }
    }
}
