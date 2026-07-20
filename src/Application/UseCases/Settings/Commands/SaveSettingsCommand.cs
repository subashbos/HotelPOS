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
                existing.UpdateFrom(settings);
                await _repository.UpdateAsync(existing);
            }
            else
            {
                await _repository.AddAsync(settings);
            }
        }
    }
}
