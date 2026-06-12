using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class CashService : ICashService
    {
        private readonly ICashRepository _repo;

        public CashService(ICashRepository repo)
        {
            _repo = repo;
        }

        public async Task<CashSession?> GetCurrentSessionAsync()
        {
            return await _repo.GetCurrentSessionAsync();
        }

        public async Task<int> OpenSessionAsync(decimal openingBalance, string username)
        {
            if (openingBalance < 0)
                throw new ArgumentException("Opening balance cannot be negative.", nameof(openingBalance));

            var active = await GetCurrentSessionAsync();
            if (active != null)
                throw new InvalidOperationException("A session is already open.");

            var session = new CashSession
            {
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = openingBalance,
                OpenedBy = username,
                Status = "Open"
            };

            await _repo.AddAsync(session);
            return session.Id;
        }

        public async Task CloseSessionAsync(decimal actualCash, string? notes, string username)
        {
            if (actualCash < 0)
                throw new ArgumentException("Actual cash cannot be negative.", nameof(actualCash));

            var session = await GetCurrentSessionAsync();
            if (session == null)
                throw new InvalidOperationException("No active session to close.");

            var sales = await GetTotalSalesForCurrentSessionAsync();

            session.ClosedAt = DateTime.UtcNow;
            session.ClosedBy = username;
            session.ClosingBalance = session.OpeningBalance + sales;
            session.ActualCash = actualCash;
            session.Notes = notes;
            session.Status = "Closed";

            await _repo.UpdateAsync(session);
        }

        public async Task<List<CashSession>> GetSessionHistoryAsync(int count = 30)
        {
            return await _repo.GetHistoryAsync(count);
        }

        public async Task<decimal> GetTotalSalesForCurrentSessionAsync()
        {
            var session = await GetCurrentSessionAsync();
            if (session == null) return 0;

            return await _repo.GetSalesTotalAsync(session.OpenedAt);
        }
    }
}
