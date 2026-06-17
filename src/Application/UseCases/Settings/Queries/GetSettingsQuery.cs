using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Settings.Queries
{
    public record GetSettingsQuery() : IRequest<SystemSetting>;

    public class GetSettingsQueryHandler : IRequestHandler<GetSettingsQuery, SystemSetting>
    {
        private readonly ISettingRepository _repository;

        public GetSettingsQueryHandler(ISettingRepository repository)
        {
            _repository = repository;
        }

        public async Task<SystemSetting> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
        {
            var settings = await _repository.GetByIdAsync(1);
            if (settings == null)
            {
                settings = new SystemSetting { Id = 1 };
                await _repository.AddAsync(settings);
            }
            return settings;
        }
    }
}
