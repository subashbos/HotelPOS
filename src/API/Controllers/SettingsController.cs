using HotelPOS.Application.DTOs.Setting;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>System / hotel profile settings — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class SettingsController : BaseApiController
    {
        private readonly ISettingService _settingService;
        private readonly Application.Interfaces.IAuthorizationService _authorization;

        public SettingsController(ISettingService settingService, Application.Interfaces.IAuthorizationService authorization)
        {
            _settingService = settingService;
            _authorization = authorization;
        }

        [HttpGet]
        public async Task<ActionResult<SettingsDto>> GetSettings()
        {
            var settings = await _settingService.GetSettingsAsync();
            var dto = new SettingsDto
            {
                HotelName = settings.HotelName,
                HotelAddress = settings.HotelAddress,
                HotelPhone = settings.HotelPhone,
                HotelGst = settings.HotelGst,
                DefaultPrinter = settings.DefaultPrinter,
                ShowPrintPreview = settings.ShowPrintPreview,
                ReceiptFormat = settings.ReceiptFormat,
                ShowGstBreakdown = settings.ShowGstBreakdown,
                ShowItemsOnBill = settings.ShowItemsOnBill,
                ShowDiscountLine = settings.ShowDiscountLine,
                ShowPhoneOnReceipt = settings.ShowPhoneOnReceipt,
                ShowThankYouFooter = settings.ShowThankYouFooter,
                EnableRoundOff = settings.EnableRoundOff,
                IsCompositionScheme = settings.IsCompositionScheme,
                EnableAutomatedBackups = settings.EnableAutomatedBackups,
                IdleTimeoutMinutes = settings.IdleTimeoutMinutes
            };

            // SMTP credentials and the offsite backup path are only needed by whoever configures
            // them (SaveSettings is Admin-only) — everything above is needed by any authenticated
            // user for day-to-day billing (receipt/GST/round-off display), so only these are gated.
            if (_authorization.HasPermission(PermissionModules.Settings))
            {
                dto.OffsiteBackupPath = settings.OffsiteBackupPath;
                dto.SmtpHost = settings.SmtpHost;
                dto.SmtpPort = settings.SmtpPort;
                dto.SmtpUsername = settings.SmtpUsername;
                dto.SmtpPasswordSet = !string.IsNullOrEmpty(settings.SmtpPassword);
                dto.SmtpUseSsl = settings.SmtpUseSsl;
                dto.SmtpFromAddress = settings.SmtpFromAddress;
            }

            return Ok(dto);
        }

        [HttpPut]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> SaveSettings([FromBody] SaveSettingsDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Preserve the existing SMTP password unless the caller explicitly supplied a new one.
            var current = await _settingService.GetSettingsAsync();
            var updated = new SystemSetting
            {
                Id = current.Id,
                HotelName = request.HotelName,
                HotelAddress = request.HotelAddress,
                HotelPhone = request.HotelPhone,
                HotelGst = request.HotelGst,
                DefaultPrinter = request.DefaultPrinter,
                ShowPrintPreview = request.ShowPrintPreview,
                ReceiptFormat = request.ReceiptFormat,
                ShowGstBreakdown = request.ShowGstBreakdown,
                ShowItemsOnBill = request.ShowItemsOnBill,
                ShowDiscountLine = request.ShowDiscountLine,
                ShowPhoneOnReceipt = request.ShowPhoneOnReceipt,
                ShowThankYouFooter = request.ShowThankYouFooter,
                EnableRoundOff = request.EnableRoundOff,
                IsCompositionScheme = request.IsCompositionScheme,
                EnableAutomatedBackups = request.EnableAutomatedBackups,
                OffsiteBackupPath = request.OffsiteBackupPath,
                IdleTimeoutMinutes = request.IdleTimeoutMinutes,
                SmtpHost = request.SmtpHost,
                SmtpPort = request.SmtpPort,
                SmtpUsername = request.SmtpUsername,
                SmtpPassword = string.IsNullOrEmpty(request.SmtpPassword) ? current.SmtpPassword : request.SmtpPassword,
                SmtpUseSsl = request.SmtpUseSsl,
                SmtpFromAddress = request.SmtpFromAddress
            };

            await _settingService.SaveSettingsAsync(updated);
            return NoContent();
        }
    }
}
