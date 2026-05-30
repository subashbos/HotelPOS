using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Domain.Interfaces
{
    public interface IPurchaseRepository
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<List<Purchase>> GetPurchasesAsync();
        Task<(List<Purchase> purchases, int totalCount)> GetPagedPurchasesAsync(int page, int pageSize, System.DateTime? from, System.DateTime? to, int? supplierId, string? itemName, string? paymentType, string? invoiceNo);
        Task AddAsync(Purchase purchase);
    }
}
