using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.Interfaces;

namespace HotelPOS.Application.UseCases.Items.Queries
{
    public record GetItemsQuery() : IRequest<List<Item>>;

    public class GetItemsQueryHandler : IRequestHandler<GetItemsQuery, List<Item>>
    {
        private readonly IItemRepository _itemRepository;

        public GetItemsQueryHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<List<Item>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
        {
            return await _itemRepository.GetAllAsync() ?? new List<Item>();
        }
    }
}
