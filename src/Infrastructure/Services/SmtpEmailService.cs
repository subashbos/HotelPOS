using System.Net;
using System.Net.Mail;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ISettingService _settingService;

        public SmtpEmailService(ISettingService settingService)
        {
            _settingService = settingService;
        }

        public async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            var settings = await _settingService.GetSettingsAsync();
            await SendEmailAsync(toAddress, subject, body, settings);
        }

        public async Task SendEmailAsync(string toAddress, string subject, string body, SystemSetting smtpSettings)
        {
            if (string.IsNullOrWhiteSpace(smtpSettings.SmtpHost))
            {
                throw new InvalidOperationException(
                    "Outgoing email is not configured. Ask an administrator to set it up under Settings > Security.");
            }

            using var client = new SmtpClient(smtpSettings.SmtpHost, smtpSettings.SmtpPort) // NOSONAR - EnableSsl below is admin-configurable via Settings > Security; some on-prem/local SMTP relays don't support TLS.
            {
                EnableSsl = smtpSettings.SmtpUseSsl,
                Credentials = string.IsNullOrEmpty(smtpSettings.SmtpUsername)
                    ? null
                    : new NetworkCredential(smtpSettings.SmtpUsername, smtpSettings.SmtpPassword)
            };

            using var message = new MailMessage(
                smtpSettings.SmtpFromAddress ?? smtpSettings.SmtpUsername ?? "noreply@hotelpos.local",
                toAddress,
                subject,
                body);

            await client.SendMailAsync(message);
        }
    }
}
