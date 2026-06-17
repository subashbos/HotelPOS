using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Tables.Queries
{
    public record GetTablesQuery() : IRequest<List<Table>>;

    public class GetTablesQueryHandler : IRequestHandler<GetTablesQuery, List<Table>>
    {
        private readonly ITableRepository _repo;

        public GetTablesQueryHandler(ITableRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Table>> Handle(GetTablesQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetAllAsync() ?? new List<Table>();
        }
    }
}
