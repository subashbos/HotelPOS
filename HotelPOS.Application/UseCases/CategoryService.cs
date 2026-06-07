using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;
        private readonly IItemRepository _itemRepo;

        public CategoryService(ICategoryRepository repo, IItemRepository itemRepo)
        {
            _repo = repo;
            _itemRepo = itemRepo;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<int> AddCategoryAsync(string name, int displayOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");

            var existing = await _repo.GetAllAsync() ?? new List<Category>();
            if (existing.Any(c => c.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{name}' already exists.");

            var category = new Category { Name = name.Trim(), DisplayOrder = displayOrder };
            return await _repo.AddAsync(category);
        }

        public async Task UpdateCategoryAsync(int id, string name, int displayOrder = 0)
        {
            if (id <= 0) throw new ArgumentException("Invalid ID");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");

            var all = await _repo.GetAllAsync() ?? new List<Category>();
            if (all.Any(c => c.Id != id && c.Name.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Category '{name}' already exists.");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Category #{id} not found.");

            existing.Name = name.Trim();
            existing.DisplayOrder = displayOrder;
            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var items = await _itemRepo.GetAllAsync() ?? new List<Item>();
            if (items.Any(i => i.CategoryId == id))
                throw new InvalidOperationException("Cannot delete category because it contains active menu items. Please reassign or delete the items first.");

            await _repo.DeleteAsync(id);
        }
    }
}
