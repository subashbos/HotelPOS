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

        [Fact]
        public async Task SendEmailAsync_NullSettings_ThrowsNullReferenceException()
        {
            // The settings-overload has no null guard - it dereferences smtpSettings.SmtpHost
            // directly, so a null SystemSetting fails fast with a NullReferenceException rather
            // than the friendlier "not configured" InvalidOperationException.
            var settingService = new Mock<ISettingService>(MockBehavior.Strict);
            var service = new SmtpEmailService(settingService.Object);

            await Assert.ThrowsAsync<NullReferenceException>(
                () => service.SendEmailAsync("guest@example.com", "Subject", "Body", null!));
        }

        [Fact]
        public async Task SendEmailAsync_InvalidRecipientFormat_ThrowsFormatException()
        {
            // With a configured host, MailMessage's own address parsing runs before any network
            // I/O is attempted, so a malformed "to" address fails synchronously and deterministically
            // (this doesn't touch the network, unlike an actual SendMailAsync failure would).
            var settingService = new Mock<ISettingService>(MockBehavior.Strict);
            var service = new SmtpEmailService(settingService.Object);
            var settings = new SystemSetting { SmtpHost = "smtp.example.com" };

            await Assert.ThrowsAsync<FormatException>(
                () => service.SendEmailAsync("not-an-email-address", "Subject", "Body", settings));
        }
    }
}
