using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Application
{
    public class SettingService : ISettingService
    {
        private readonly ISettingRepository _repository;

        public SettingService(ISettingRepository repository)
        {
            _repository = repository;
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
            var existing = await _repository.GetByIdAsync(1);
            if (existing != null)
            {
                existing.HotelName        = settings.HotelName;
                existing.HotelAddress     = settings.HotelAddress;
                existing.HotelPhone       = settings.HotelPhone;
                existing.HotelGst         = settings.HotelGst;
                existing.DefaultPrinter   = settings.DefaultPrinter;
                existing.ShowPrintPreview = settings.ShowPrintPreview;
                existing.ReceiptFormat    = settings.ReceiptFormat;

                // Receipt display flags — previously silently dropped
                existing.ShowGstBreakdown   = settings.ShowGstBreakdown;
                existing.ShowItemsOnBill    = settings.ShowItemsOnBill;
                existing.ShowDiscountLine   = settings.ShowDiscountLine;
                existing.ShowPhoneOnReceipt = settings.ShowPhoneOnReceipt;
                existing.ShowThankYouFooter = settings.ShowThankYouFooter;

                await _repository.UpdateAsync(existing);
            }
            else
            {
                await _repository.AddAsync(settings);
            }
        }
    }
}
