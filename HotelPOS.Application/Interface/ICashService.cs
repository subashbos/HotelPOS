using HotelPOS.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HotelPOS.Application.Interface
{
    public interface ICashService
    {
        Task<CashSession?> GetCurrentSessionAsync();
        Task<int> OpenSessionAsync(decimal openingBalance, string username);
        Task CloseSessionAsync(decimal actualCash, string? notes, string username);
        Task<List<CashSession>> GetSessionHistoryAsync(int count = 30);
        Task<decimal> GetTotalSalesForCurrentSessionAsync();
    }
}
