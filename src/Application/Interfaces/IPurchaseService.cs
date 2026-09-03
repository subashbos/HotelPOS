using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IPurchaseService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<(List<Purchase> purchases, int totalCount)> GetPagedPurchasesAsync(int page, int pageSize, PurchaseQueryFilter? filter = null);
        Task<Purchase?> GetPurchaseByIdAsync(int id);
        Task SavePurchaseAsync(Purchase purchase);
        Task UpdatePurchaseAsync(Purchase purchase);
        Task DeletePurchaseAsync(int id);
    }
}
