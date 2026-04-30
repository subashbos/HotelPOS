using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests
{
    public class SessionViewModelTests
    {
        private readonly Mock<ICashService> _mockCashService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly SessionViewModel _vm;

        public SessionViewModelTests()
        {
            _vm = new SessionViewModel(_mockCashService.Object, _mockNotif.Object);

            // Default setups to avoid null refs
            _mockCashService.Setup(s => s.GetSessionHistoryAsync(It.IsAny<int>()))
                           .ReturnsAsync(new List<CashSession>());
            _mockCashService.Setup(s => s.GetTotalSalesForCurrentSessionAsync())
                           .ReturnsAsync(0m);
        }

        [Fact]
        public async Task InitializeAsync_LoadsHistoryAndStatus()
        {
            // Arrange
            var session = new CashSession { Id = 1, Status = "Open" };
            var history = new List<CashSession> { session };
            
            _mockCashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(session);
            _mockCashService.Setup(s => s.GetSessionHistoryAsync(It.IsAny<int>())).ReturnsAsync(history);
            _mockCashService.Setup(s => s.GetTotalSalesForCurrentSessionAsync()).ReturnsAsync(200m);

            // Act
            await _vm.InitializeAsync();

            // Assert
            Assert.True(_vm.IsSessionOpen);
            Assert.Equal(session, _vm.CurrentSession);
            Assert.Single(_vm.SessionHistory);
            Assert.Equal(200, _vm.ExpectedCash - session.OpeningBalance);
        }

        [Fact]
        public async Task OpenSessionCommand_ValidBalance_CallsServiceAndNotifies()
        {
            // Arrange
            _vm.OpeningBalance = 500;
            _mockCashService.Setup(s => s.OpenSessionAsync(It.IsAny<decimal>(), It.IsAny<string>()))
                           .ReturnsAsync(1);

            // Act
            await _vm.OpenSessionCommand.ExecuteAsync(null);

            // Assert
            _mockCashService.Verify(s => s.OpenSessionAsync(500m, It.IsAny<string>()), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task OpenSessionCommand_NegativeBalance_ShowsError()
        {
            // Arrange
            _vm.OpeningBalance = -10;

            // Act
            await _vm.OpenSessionCommand.ExecuteAsync(null);

            // Assert
            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("negative"))), Times.Once);
            _mockCashService.Verify(s => s.OpenSessionAsync(It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CloseSessionCommand_CallsServiceAndRefreshes()
        {
            // Arrange
            var session = new CashSession { Id = 1, Status = "Open" };
            _mockCashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(session);
            _vm.ActualCash = 1200;
            _vm.Notes = "All good";

            // Act
            await _vm.CloseSessionCommand.ExecuteAsync(null);

            // Assert
            _mockCashService.Verify(s => s.CloseSessionAsync(1200m, "All good", It.IsAny<string>()), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
        }
    }
}
