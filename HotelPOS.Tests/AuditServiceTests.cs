using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class AuditServiceTests
    {
        private readonly Mock<IAuditRepository> _repoMock;
        private readonly Mock<IUserContext> _userContextMock;
        private readonly AuditService _service;

        public AuditServiceTests()
        {
            _repoMock = new Mock<IAuditRepository>();
            _userContextMock = new Mock<IUserContext>();
            _service = new AuditService(_repoMock.Object, _userContextMock.Object);
        }

        [Fact]
        public async Task LogActionAsync_Should_Populate_Log_Correctly()
        {
            // Arrange
            _userContextMock.Setup(u => u.CurrentUsername).Returns("testuser");
            AuditLog? capturedLog = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .Callback<AuditLog>(log => capturedLog = log)
                     .Returns(Task.CompletedTask);

            // Act
            await _service.LogActionAsync("Item", 123, "Update", "Price changed");

            // Assert
            Assert.NotNull(capturedLog);
            Assert.Equal("Item", capturedLog.EntityName);
            Assert.Equal(123, capturedLog.EntityId);
            Assert.Equal("Update", capturedLog.Action);
            Assert.Equal("Price changed", capturedLog.Details);
            Assert.Equal("testuser", capturedLog.Username);
            Assert.True((DateTime.UtcNow - capturedLog.Timestamp).TotalSeconds < 5);
        }

        [Fact]
        public async Task GetLogsAsync_Should_Call_Repository()
        {
            // Arrange
            var logs = new List<AuditLog> { new AuditLog { Action = "Test" } };
            _repoMock.Setup(r => r.GetLogsAsync(null, null)).ReturnsAsync(logs);

            // Act
            var result = await _service.GetLogsAsync();

            // Assert
            Assert.Equal(logs, result);
            _repoMock.Verify(r => r.GetLogsAsync(null, null), Times.Once);
        }
    }
}
