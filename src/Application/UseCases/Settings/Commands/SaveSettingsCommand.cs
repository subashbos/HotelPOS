using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Settings.Commands
{
    public record SaveSettingsCommand(SystemSetting Settings) : IRequest;

    public class SaveSettingsCommandHandler : IRequestHandler<SaveSettingsCommand>
    {
        private readonly ISettingRepository _repository;

        public SaveSettingsCommandHandler(ISettingRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(SaveSettingsCommand request, CancellationToken cancellationToken)
        {
            var settings = request.Settings;
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
                existing.ShowGstBreakdown = settings.ShowGstBreakdown;
                existing.ShowItemsOnBill = settings.ShowItemsOnBill;
                existing.ShowDiscountLine = settings.ShowDiscountLine;
                existing.ShowPhoneOnReceipt = settings.ShowPhoneOnReceipt;
                existing.ShowThankYouFooter = settings.ShowThankYouFooter;
                existing.EnableRoundOff = settings.EnableRoundOff;
                existing.IsCompositionScheme = settings.IsCompositionScheme;
                existing.EnableAutomatedBackups = settings.EnableAutomatedBackups;
                existing.OffsiteBackupPath = settings.OffsiteBackupPath;

                await _repository.UpdateAsync(existing);
            }
            else
            {
                await _repository.AddAsync(settings);
            }
        }
    }
}
