using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.UseCases.Audit.Commands;
using HotelPOS.Application.UseCases.Audit.Queries;

namespace HotelPOS.Application.UseCases
{
    public class AuditService : IAuditService
    {
        private readonly IMediator _mediator;

        public AuditService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task LogActionAsync(string entityName, int entityId, string action, string? details = null)
        {
            await _mediator.Send(new LogActionCommand(entityName, entityId, action, details));
        }

        public async Task<List<HotelPOS.Application.DTOs.Audit.AuditLogDto>> GetLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await _mediator.Send(new GetLogsQuery(from, to));
        }
    }
}
