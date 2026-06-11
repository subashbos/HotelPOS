using MediatR;
using HotelPOS.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelPOS.Application.UseCases.Items.Commands
{
    public record DeleteItemCommand(int Id) : IRequest<bool>;

    public class DeleteItemCommandHandler : IRequestHandler<DeleteItemCommand, bool>
    {
        private readonly IItemRepository _itemRepository;

        public DeleteItemCommandHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<bool> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
        {
            if (request.Id <= 0)
                throw new ArgumentException("Invalid item ID.", nameof(request));

            var item = await _itemRepository.GetByIdAsync(request.Id);
            if (item == null)
                throw new KeyNotFoundException($"Item #{request.Id} not found.");

            await _itemRepository.DeleteAsync(request.Id);
            return true;
        }
    }
}
