using HotelPOS.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interface
{
    public interface IPurchaseService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<List<Purchase>> GetPurchasesAsync();
        Task SavePurchaseAsync(Purchase purchase);
    }
}
