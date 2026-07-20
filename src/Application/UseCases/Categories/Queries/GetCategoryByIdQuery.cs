using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Categories.Queries
{
    public record GetCategoryByIdQuery(int Id) : IRequest<Category?>;

    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Category?>
    {
        private readonly ICategoryRepository _repository;

        public GetCategoryByIdQueryHandler(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Category?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
