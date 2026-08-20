using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class CashServiceTests
    {
        private readonly Mock<ICashRepository> _repoMock;
        private readonly CashService _service;

        public CashServiceTests()
        {
            _repoMock = new Mock<ICashRepository>();
            _service = new CashService(_repoMock.Object);
        }

        [Fact]
        public async Task OpenSessionAsync_WhenNoActiveSession_ShouldCreateSession()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            // Act
            var sessionId = await _service.OpenSessionAsync(1000, "admin");

            // Assert
            _repoMock.Verify(r => r.AddAsync(It.Is<CashSession>(s => s.OpeningBalance == 1000 && s.OpenedBy == "admin")), Times.Once);
        }

        [Fact]
        public async Task OpenSessionAsync_WhenActiveSessionExists_ShouldThrowException()
        {
            // Arrange
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(new CashSession());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.OpenSessionAsync(1000, "admin"));
        }

        [Fact]
        public async Task CloseSessionAsync_ShouldCalculateBalanceAndClose()
        {
            // Arrange
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-5), OpeningBalance = 500, OpenedBy = "admin" };
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repoMock.Setup(r => r.GetSalesTotalAsync(It.IsAny<DateTime>())).ReturnsAsync(2000);

            // Act
            await _service.CloseSessionAsync(2500, "Clean close", "admin");

            // Assert
            Assert.Equal(2500, session.ClosingBalance); // 500 + 2000
            Assert.Equal("Closed", session.Status);
            _repoMock.Verify(r => r.UpdateAsync(session), Times.Once);
        }

        [Fact]
        public async Task CashService_OpenSessionAsync_NegativeOpeningBalance_ThrowsAtServiceLayer()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.OpenSessionAsync(-100m, "admin"));
            Assert.Contains("Opening balance cannot be negative", ex.Message);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<CashSession>()), Times.Never);
        }

        [Fact]
        public async Task CashService_CloseSessionAsync_NegativeActualCash_Throws()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CloseSessionAsync(-50m, "Notes", "admin"));
            Assert.Contains("Actual cash amount cannot be negative", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<CashSession>()), Times.Never);
        }

        [Fact]
        public async Task CloseSessionAsync_NoActiveSession_ThrowsInvalidOperationException()
        {
            // Arrange - repository has no active (already-closed) session to close.
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CloseSessionAsync(100m, "notes", "admin"));
            Assert.Contains("No active session to close", ex.Message);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<CashSession>()), Times.Never);
        }

        [Fact]
        public async Task GetCurrentSessionAsync_ReturnsFromRepository()
        {
            var session = new CashSession { Id = 3 };
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);

            var result = await _service.GetCurrentSessionAsync();

            Assert.Same(session, result);
        }

        [Fact]
        public async Task GetSessionHistoryAsync_ReturnsFromRepository()
        {
            var history = new List<CashSession> { new CashSession { Id = 1 }, new CashSession { Id = 2 } };
            _repoMock.Setup(r => r.GetHistoryAsync(10)).ReturnsAsync(history);

            var result = await _service.GetSessionHistoryAsync(10);

            Assert.Same(history, result);
        }

        [Fact]
        public async Task GetTotalSalesForCurrentSessionAsync_NoActiveSession_ReturnsZero()
        {
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync((CashSession?)null);

            var result = await _service.GetTotalSalesForCurrentSessionAsync();

            Assert.Equal(0, result);
        }

        [Fact]
        public async Task GetTotalSalesForCurrentSessionAsync_ActiveSession_ReturnsSalesTotal()
        {
            var session = new CashSession { OpenedAt = DateTime.UtcNow.AddHours(-2) };
            _repoMock.Setup(r => r.GetCurrentSessionAsync()).ReturnsAsync(session);
            _repoMock.Setup(r => r.GetSalesTotalAsync(session.OpenedAt)).ReturnsAsync(750m);

            var result = await _service.GetTotalSalesForCurrentSessionAsync();

            Assert.Equal(750m, result);
        }
    }
}

