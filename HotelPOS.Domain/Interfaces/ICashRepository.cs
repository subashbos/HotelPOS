namespace HotelPOS.Domain.Interfaces
{
    public interface ICashRepository
    {
        Task<CashSession?> GetCurrentSessionAsync();
        Task AddAsync(CashSession session);
        Task UpdateAsync(CashSession session);
        Task<List<CashSession>> GetHistoryAsync(int count);
        Task<decimal> GetSalesTotalAsync(DateTime since);
    }
}
