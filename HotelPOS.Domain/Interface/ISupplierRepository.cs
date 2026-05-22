using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Domain.Interface
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier?> GetByNameAsync(string name);
        Task AddAsync(Supplier supplier);
        Task UpdateAsync(Supplier supplier);
        Task DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int excludeId = 0);
    }
}
