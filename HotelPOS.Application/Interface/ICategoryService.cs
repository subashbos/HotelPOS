using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<int> AddCategoryAsync(string name);
        Task UpdateCategoryAsync(int id, string name);
        Task DeleteCategoryAsync(int id);
    }
}
