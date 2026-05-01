using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
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
    }
}
