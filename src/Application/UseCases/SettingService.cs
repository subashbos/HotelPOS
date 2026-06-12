using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class SettingService : ISettingService
    {
        private readonly ISettingRepository _repository;
        private readonly IAuthorizationService _authorization;

        public SettingService(ISettingRepository repository, IAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        public async Task<SystemSetting> GetSettingsAsync()
        {
            var settings = await _repository.GetByIdAsync(1);
            if (settings == null)
            {
                settings = new SystemSetting { Id = 1 };
                await _repository.AddAsync(settings);
            }
            return settings;
        }

        public async Task SaveSettingsAsync(SystemSetting settings)
        {
            _authorization.EnsurePermission("Settings");

            var existing = await _repository.GetByIdAsync(1);
            if (existing != null)
            {
                existing.HotelName = settings.HotelName;
                existing.HotelAddress = settings.HotelAddress;
                existing.HotelPhone = settings.HotelPhone;
                existing.HotelGst = settings.HotelGst;
                existing.DefaultPrinter = settings.DefaultPrinter;
                existing.ShowPrintPreview = settings.ShowPrintPreview;
                existing.ReceiptFormat = settings.ReceiptFormat;

                // Receipt display flags — previously silently dropped
                existing.ShowGstBreakdown = settings.ShowGstBreakdown;
                existing.ShowItemsOnBill = settings.ShowItemsOnBill;
                existing.ShowDiscountLine = settings.ShowDiscountLine;
                existing.ShowPhoneOnReceipt = settings.ShowPhoneOnReceipt;
                existing.ShowThankYouFooter = settings.ShowThankYouFooter;

                // Billing options
                existing.EnableRoundOff = settings.EnableRoundOff;
                existing.IsCompositionScheme = settings.IsCompositionScheme;

                await _repository.UpdateAsync(existing);
            }
            else
            {
                await _repository.AddAsync(settings);
            }
        }
    }
}
