using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.UseCases.Categories.Commands;
using HotelPOS.Application.UseCases.Categories.Queries;
using FluentValidation;
using HotelPOS.Application.Common.Validators;

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Application.UseCases
{
    public class CategoryService : ICategoryService
    {
        private readonly IMediator? _mediator;
        private readonly ICategoryRepository? _repo;
        private readonly IItemRepository? _itemRepo;
        private readonly IValidator<Category> _validator;

        public CategoryService(IMediator mediator)
        {
            _mediator = mediator;
            _validator = new CategoryValidator();
        }

        public CategoryService(ICategoryRepository repo, IItemRepository itemRepo, IValidator<Category>? validator = null)
        {
            _repo = repo;
            _itemRepo = itemRepo;
            _validator = validator ?? new CategoryValidator();
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetCategoriesQuery());

            return await _repo!.GetAllAsync() ?? new List<Category>();
        }

        public async Task<int> AddCategoryAsync(string name, int displayOrder = 0)
        {
            if (_mediator != null)
                return await _mediator.Send(new CreateCategoryCommand(name, displayOrder));

            var trimmedName = name?.Trim() ?? string.Empty;
            var category = new Category { Name = trimmedName, DisplayOrder = displayOrder };
            var result = _validator.Validate(category);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            var existing = await _repo!.GetAllAsync() ?? new List<Category>();
            if (existing.Any(c => c.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{name}' already exists.");

            return await _repo.AddAsync(category);
        }

        public async Task UpdateCategoryAsync(int id, string name, int displayOrder = 0)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new UpdateCategoryCommand(id, name, displayOrder));
                return;
            }

            if (id <= 0) throw new ArgumentException("Invalid ID");

            var trimmedName = name?.Trim() ?? string.Empty;
            var category = new Category { Id = id, Name = trimmedName, DisplayOrder = displayOrder };
            var result = _validator.Validate(category);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors[0].ErrorMessage);

            var all = await _repo!.GetAllAsync() ?? new List<Category>();
            if (all.Any(c => c.Id != id && c.Name.Trim().Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{name}' already exists.");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Category #{id} not found.");

            existing.Name = trimmedName;
            existing.DisplayOrder = displayOrder;
            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteCategoryCommand(id));
                return;
            }

            var items = await _itemRepo!.GetAllAsync() ?? new List<Item>();
            if (items.Any(i => i.CategoryId == id))
                throw new InvalidOperationException("Cannot delete category because it contains active menu items. Please reassign or delete the items first.");

            await _repo!.DeleteAsync(id);
        }
    }
}
