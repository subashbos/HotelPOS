using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Categories.Queries
{
    public record GetCategoriesQuery() : IRequest<List<Category>>;

    public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<Category>>
    {
        private readonly ICategoryRepository _repo;

        public GetCategoriesQueryHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetAllAsync() ?? new List<Category>();
        }
    }
}
