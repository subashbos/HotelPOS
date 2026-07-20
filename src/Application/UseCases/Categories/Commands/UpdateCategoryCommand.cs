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
    public record UpdateCategoryCommand(int Id, string Name, int DisplayOrder = 0) : IRequest;

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
    {
        private readonly ICategoryRepository _repo;

        public UpdateCategoryCommandHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
        {
            var all = await _repo.GetAllAsync() ?? new List<Category>();
            if (all.Any(c => c.Id != request.Id && c.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{request.Name}' already exists.");

            var existing = await _repo.GetByIdAsync(request.Id);
            if (existing == null) throw new KeyNotFoundException($"Category #{request.Id} not found.");

            existing.Name = request.Name.Trim();
            existing.DisplayOrder = request.DisplayOrder;
            await _repo.UpdateAsync(existing);
        }
    }
}
