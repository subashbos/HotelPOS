using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toAddress, string subject, string body);

        /// <summary>Sends using the given SMTP settings directly, bypassing the saved configuration.
        /// Used to let an admin verify SMTP settings before saving them.</summary>
        Task SendEmailAsync(string toAddress, string subject, string body, SystemSetting smtpSettings);
    }
}
