using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface ISupplierService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task SaveSupplierAsync(Supplier supplier);
        Task DeleteSupplierAsync(int id);
        Task<bool> ValidateSupplierNameUniqueAsync(string name, int excludeId = 0);
    }
}
