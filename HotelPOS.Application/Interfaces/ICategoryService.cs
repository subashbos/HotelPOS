using HotelPOS.Domain;

namespace HotelPOS.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<int> AddCategoryAsync(string name, int displayOrder = 0);
        Task UpdateCategoryAsync(int id, string name, int displayOrder = 0);
        Task DeleteCategoryAsync(int id);
    }
}
