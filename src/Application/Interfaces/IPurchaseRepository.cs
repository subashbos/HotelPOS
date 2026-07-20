using HotelPOS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IPurchaseRepository
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<List<Purchase>> GetPurchasesAsync();
        Task<(List<Purchase> purchases, int totalCount)> GetPagedPurchasesAsync(int page, int pageSize, PurchaseQueryFilter? filter = null);
        Task AddAsync(Purchase purchase);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }

    /// <summary>Optional filter criteria for <see cref="IPurchaseRepository.GetPagedPurchasesAsync"/>.</summary>
    public record PurchaseQueryFilter(
        System.DateTime? From = null,
        System.DateTime? To = null,
        int? SupplierId = null,
        string? ItemName = null,
        string? PaymentType = null,
        string? InvoiceNo = null
    );
}
