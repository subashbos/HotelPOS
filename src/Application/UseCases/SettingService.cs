#nullable enable

using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Settings.Commands;
using HotelPOS.Application.UseCases.Settings.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class SettingService : ISettingService
    {
        private readonly IMediator? _mediator;
        private readonly ISettingRepository? _repository;
        private readonly IAuthorizationService? _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public SettingService(IMediator mediator, IAuthorizationService authorization)
        {
            _mediator = mediator;
            _authorization = authorization;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public SettingService(ISettingRepository repository, IAuthorizationService authorization, bool isTest)
        {
            _repository = repository;
            _authorization = authorization;
        }

        public async Task<SystemSetting> GetSettingsAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetSettingsQuery());

            var settings = await _repository!.GetByIdAsync(1);
            if (settings == null)
            {
                settings = new SystemSetting { Id = 1 };
                await _repository.AddAsync(settings);
            }
            return settings;
        }

        public async Task SaveSettingsAsync(SystemSetting settings)
        {
            _authorization?.EnsurePermission("Settings");

            if (_mediator != null)
            {
                await _mediator.Send(new SaveSettingsCommand(settings));
                await _mediator.Publish(new EntityActionEvent("Setting", settings.Id == 0 ? 1 : settings.Id, "Update",
                    $"Hotel profile/receipt/billing settings updated (Hotel: {settings.HotelName})"));
                return;
            }

            var existing = await _repository!.GetByIdAsync(1);
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
