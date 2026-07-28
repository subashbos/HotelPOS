using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
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
        private readonly IAuthorizationService _authorization;

        public DeleteItemCommandHandler(IItemRepository itemRepository, IAuthorizationService authorization)
        {
            _itemRepository = itemRepository;
            _authorization = authorization;
        }

        public async Task<bool> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Items);

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
