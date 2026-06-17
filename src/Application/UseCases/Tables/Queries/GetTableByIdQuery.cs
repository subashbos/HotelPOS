using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Tables.Queries
{
    public record GetTableByIdQuery(int Id) : IRequest<Table?>;

    public class GetTableByIdQueryHandler : IRequestHandler<GetTableByIdQuery, Table?>
    {
        private readonly ITableRepository _repository;

        public GetTableByIdQueryHandler(ITableRepository repository)
        {
            _repository = repository;
        }

        public async Task<Table?> Handle(GetTableByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
