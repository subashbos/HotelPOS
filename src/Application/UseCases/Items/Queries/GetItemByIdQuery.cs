using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.Interfaces;

namespace HotelPOS.Application.UseCases.Items.Queries
{
    public record GetItemByIdQuery(int Id) : IRequest<Item?>;

    public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, Item?>
    {
        private readonly IItemRepository _itemRepository;

        public GetItemByIdQueryHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<Item?> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
        {
            if (request.Id <= 0) return null;
            return await _itemRepository.GetByIdAsync(request.Id);
        }
    }
}
