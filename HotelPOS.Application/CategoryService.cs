using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Application
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repo;

        public CategoryService(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<int> AddCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");

            var category = new Category { Name = name.Trim() };
            return await _repo.AddAsync(category);
        }

        public async Task UpdateCategoryAsync(int id, string name)
        {
            if (id <= 0) throw new ArgumentException("Invalid ID");
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) throw new KeyNotFoundException($"Category #{id} not found.");

            existing.Name = name.Trim();
            await _repo.UpdateAsync(existing);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            await _repo.DeleteAsync(id);
        }
    }
}
