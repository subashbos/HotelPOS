using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Settings.Commands;
using HotelPOS.Application.UseCases.Settings.Queries;
using HotelPOS.Domain.Entities;
using MediatR;
using Moq;
using System.Threading;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers SettingService authorization enforcement and the GetSettingsAsync mediator-DI
    /// path, neither of which had a dedicated test previously (SettingServiceLoopholeTests
    /// only exercised SaveSettingsAsync's mediator path and the legacy repository path).
    /// </summary>
    public class SettingServiceAuthorizationTests
    {
        private readonly Mock<ISettingRepository> _repo = new();

        [Fact]
        public async Task SaveSettingsAsync_WhenUnauthorized_LegacyPath_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var service = new SettingService(_repo.Object, auth.Object, isTest: true);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.SaveSettingsAsync(new SystemSetting()));

            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SaveSettingsAsync_WhenUnauthorized_MediatorPath_Throws()
        {
            var auth = TestAuthorization.DenyAll();
            var mediator = new Mock<IMediator>();
            var service = new SettingService(mediator.Object, auth.Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.SaveSettingsAsync(new SystemSetting()));

            mediator.Verify(m => m.Send(It.IsAny<SaveSettingsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SaveSettingsAsync_WhenAuthorized_EnsuresSettingsPermission()
        {
            var auth = TestAuthorization.AllowAll();
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new SystemSetting { Id = 1 });
            var service = new SettingService(_repo.Object, auth.Object, isTest: true);

            await service.SaveSettingsAsync(new SystemSetting { HotelName = "Test" });

            auth.Verify(a => a.EnsurePermission(PermissionModules.Settings), Times.Once);
        }

        [Fact]
        public async Task GetSettingsAsync_DoesNotRequireAuthorization()
        {
            // GetSettingsAsync has no authorization check by design (read-only settings are
            // needed by any authenticated screen, e.g. receipt formatting). This test locks
            // in that behavior so a future change doesn't silently make it stricter or looser.
            var auth = TestAuthorization.DenyAll();
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new SystemSetting { Id = 1, HotelName = "Grand Hotel" });
            var service = new SettingService(_repo.Object, auth.Object, isTest: true);

            var result = await service.GetSettingsAsync();

            Assert.Equal("Grand Hotel", result.HotelName);
        }

        [Fact]
        public async Task GetSettingsAsync_MediatorPath_DelegatesToMediatorAndDoesNotTouchRepository()
        {
            var expected = new SystemSetting { Id = 1, HotelName = "Mediator Hotel" };
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<GetSettingsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var service = new SettingService(mediator.Object, TestAuthorization.AllowAll().Object);

            var result = await service.GetSettingsAsync();

            Assert.Same(expected, result);
            mediator.Verify(m => m.Send(It.IsAny<GetSettingsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
            _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
