using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Services;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    /// <summary>
    /// Covers the deterministic, mockable branches of SmtpEmailService: the
    /// configuration guard clause and the settings-lookup delegation. Actually
    /// dispatching mail via SmtpClient needs a live SMTP endpoint and is out of
    /// scope for a unit test.
    /// </summary>
    public class SmtpEmailServiceTests
    {
        [Fact]
        public async Task SendEmailAsync_MissingSmtpHost_ThrowsInvalidOperationException()
        {
            var settingService = new Mock<ISettingService>();
            settingService.Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(new SystemSetting { SmtpHost = null });
            var service = new SmtpEmailService(settingService.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendEmailAsync("guest@example.com", "Subject", "Body"));

            Assert.Contains("not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
            settingService.Verify(s => s.GetSettingsAsync(), Times.Once);
        }

        [Fact]
        public async Task SendEmailAsync_BlankSmtpHost_ThrowsInvalidOperationException()
        {
            var settingService = new Mock<ISettingService>(MockBehavior.Strict);
            var service = new SmtpEmailService(settingService.Object);
            var settings = new SystemSetting { SmtpHost = "   " };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendEmailAsync("guest@example.com", "Subject", "Body", settings));

            Assert.Contains("Settings > Security", ex.Message);
        }

        [Fact]
        public async Task SendEmailAsync_DelegatesToSettingsOverload_UsingSavedSettings()
        {
            var settingService = new Mock<ISettingService>();
            settingService.Setup(s => s.GetSettingsAsync())
                .ReturnsAsync(new SystemSetting { SmtpHost = "" });
            var service = new SmtpEmailService(settingService.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SendEmailAsync("guest@example.com", "Subject", "Body"));

            settingService.Verify(s => s.GetSettingsAsync(), Times.Once);
        }
    }
}
