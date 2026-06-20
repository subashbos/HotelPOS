using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers CashService edge cases missing from CashServiceTests.cs:
    /// close with no session, negative opening balance, GetTotal with no session,
    /// session history count, and closing balance formula verification.
    /// </summary>
    public class CashServiceLoopholeTests
    {
        private readonly Mock<ICashRepository> _repo = new();
        private readonly CashService _service;

        public CashServiceLoopholeTests()
        {
            _service = new CashService(_repo.Object);
        }

        // ── CloseSessionAsync with no open session ───────────────────────────

        [Fact]
        public async Task CloseSessionAsync_NoActiveSession_ThrowsInvalidOperationException()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CloseSessionAsync(500m, null, "admin"));
        }

        // ── OpenSessionAsync with negative balance ───────────────────────────

        [Fact]
        public async Task OpenSessionAsync_NegativeOpeningBalance_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.OpenSessionAsync(-100m, "admin"));
        }

        [Fact]
        public async Task OpenSessionAsync_ZeroOpeningBalance_Succeeds()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            await _service.OpenSessionAsync(0m, "admin");

            _repo.Verify(r => r.AddAsync(It.Is<CashSession>(s => s.OpeningBalance == 0m)), Times.Once);
        }

        [Fact]
        public async Task OpenSessionAsync_SetsStatusToOpen()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);
            CashSession? captured = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<CashSession>()))
                 .Callback<CashSession>(s => captured = s);

            await _service.OpenSessionAsync(1000m, "cashier1");

            Assert.NotNull(captured);
            Assert.Equal("Open", captured!.Status);
            Assert.Equal("cashier1", captured.OpenedBy);
        }

        [Fact]
        public async Task OpenSessionAsync_SetsOpenedAtToUtcNow()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);
            var before = DateTime.UtcNow.AddSeconds(-1);
            CashSession? captured = null;
            _repo.Setup(r => r.AddAsync(It.IsAny<CashSession>()))
                 .Callback<CashSession>(s => captured = s);

            await _service.OpenSessionAsync(500m, "admin");

            Assert.True(captured!.OpenedAt >= before);
        }

        // ── GetTotalSalesForCurrentSessionAsync with no session ──────────────

        [Fact]
        public async Task GetTotalSalesForCurrentSessionAsync_NoSession_ReturnsZero()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            var total = await _service.GetTotalSalesForCurrentSessionAsync();

            Assert.Equal(0m, total);
        }

        [Fact]
        public async Task GetTotalSalesForCurrentSessionAsync_WithSession_ReturnsSalesTotal()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-2) };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repo.Setup(r => r.GetSalesTotalAsync(session.OpenedAt)).ReturnsAsync(3500m);

            var total = await _service.GetTotalSalesForCurrentSessionAsync();

            Assert.Equal(3500m, total);
        }

        // ── CloseSessionAsync fields ─────────────────────────────────────────

        [Fact]
        public async Task CloseSessionAsync_NegativeActualCash_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CloseSessionAsync(-50m, "notes", "admin"));
        }

        [Fact]
        public async Task CloseSessionAsync_SetsClosedByAndNotes()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-3), OpeningBalance = 200m };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repo.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(1000m);

            await _service.CloseSessionAsync(1200m, "End of day", "manager1");

            Assert.Equal("manager1", session.ClosedBy);
            Assert.Equal("End of day", session.Notes);
            Assert.Equal("Closed", session.Status);
        }

        [Fact]
        public async Task CloseSessionAsync_ActualCashStoredSeparatelyFromClosingBalance()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-1), OpeningBalance = 500m };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repo.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(2000m);

            // Actual cash counted is different from expected closing balance
            await _service.CloseSessionAsync(actualCash: 2400m, notes: null, username: "admin");

            // ClosingBalance = OpeningBalance + Sales = 500 + 2000 = 2500
            Assert.Equal(2500m, session.ClosingBalance);
            // ActualCash is what was physically counted
            Assert.Equal(2400m, session.ActualCash);
        }

        [Fact]
        public async Task CloseSessionAsync_NullNotes_DoesNotThrow()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow, OpeningBalance = 0m };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repo.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(0m);

            var ex = await Record.ExceptionAsync(
                () => _service.CloseSessionAsync(0m, null, "admin"));
            Assert.Null(ex);
        }

        [Fact]
        public async Task CloseSessionAsync_SetsClosedAtTimestamp()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-1), OpeningBalance = 100m };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repo.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(500m);
            var before = DateTime.UtcNow.AddSeconds(-1);

            await _service.CloseSessionAsync(600m, null, "admin");

            Assert.True(session.ClosedAt >= before);
        }

        // ── GetSessionHistoryAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetSessionHistoryAsync_DefaultCount_PassesThirtyToRepo()
        {
            _repo.Setup(r => r.GetHistoryAsync(30)).ReturnsAsync(new List<CashSession>());

            await _service.GetSessionHistoryAsync();

            _repo.Verify(r => r.GetHistoryAsync(30), Times.Once);
        }

        [Fact]
        public async Task GetSessionHistoryAsync_CustomCount_PassesCountToRepo()
        {
            _repo.Setup(r => r.GetHistoryAsync(5)).ReturnsAsync(new List<CashSession>());

            await _service.GetSessionHistoryAsync(5);

            _repo.Verify(r => r.GetHistoryAsync(5), Times.Once);
        }

        [Fact]
        public async Task GetSessionHistoryAsync_ReturnsRepoResult()
        {
            var sessions = new List<CashSession>
            {
                new CashSession { Id = 1, Status = "Closed" },
                new CashSession { Id = 2, Status = "Closed" }
            };
            _repo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(sessions);

            var result = await _service.GetSessionHistoryAsync();

            Assert.Equal(2, result.Count);
        }

        // ── GetCurrentSessionAsync ───────────────────────────────────────────

        [Fact]
        public async Task GetCurrentSessionAsync_NoSession_ReturnsNull()
        {
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            var result = await _service.GetCurrentSessionAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCurrentSessionAsync_WithSession_ReturnsSession()
        {
            var session = new CashSession { Id = 7, Status = "Open" };
            _repo.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);

            var result = await _service.GetCurrentSessionAsync();

            Assert.NotNull(result);
            Assert.Equal(7, result!.Id);
        }
    }
}

