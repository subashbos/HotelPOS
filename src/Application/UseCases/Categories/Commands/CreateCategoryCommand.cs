using MediatR;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HotelPOS.Application.UseCases.Categories.Commands
{
    public record CreateCategoryCommand(string Name, int DisplayOrder = 0) : IRequest<int>;

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int>
    {
        private readonly ICategoryRepository _repo;
        private readonly IAuthorizationService _authorization;

        public CreateCategoryCommandHandler(ICategoryRepository repo, IAuthorizationService authorization)
        {
            _repo = repo;
            _authorization = authorization;
        }

        public async Task<int> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            _authorization.EnsurePermission(PermissionModules.Categories);

            var existing = await _repo.GetAllAsync() ?? new List<Category>();
            if (existing.Any(c => c.Name.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{request.Name}' already exists.");

            var category = new Category { Name = request.Name.Trim(), DisplayOrder = request.DisplayOrder };
            var result = await _repo.AddAsync(category);
            return result.Id;
        }
    }
}
