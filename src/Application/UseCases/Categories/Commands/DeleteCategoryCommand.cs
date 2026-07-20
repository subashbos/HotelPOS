using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HotelPOS.Application.UseCases.Categories.Commands
{
    public record DeleteCategoryCommand(int Id) : IRequest;

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly ICategoryRepository _repo;
        private readonly IItemRepository _itemRepo;

        public DeleteCategoryCommandHandler(ICategoryRepository repo, IItemRepository itemRepo)
        {
            _repo = repo;
            _itemRepo = itemRepo;
        }

        public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var items = await _itemRepo.GetAllAsync() ?? new List<Item>();
            if (items.Any(i => i.CategoryId == request.Id))
                throw new InvalidOperationException("Cannot delete category because it contains active menu items. Please reassign or delete the items first.");

            await _repo.DeleteAsync(request.Id);
        }
    }
}
