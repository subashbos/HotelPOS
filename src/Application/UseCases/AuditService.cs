using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.UseCases.Audit.Commands;
using HotelPOS.Application.UseCases.Audit.Queries;

namespace HotelPOS.Application.UseCases
{
    public class AuditService : IAuditService
    {
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorization;

        public AuditService(IMediator mediator, IAuthorizationService authorization)
        {
            _mediator = mediator;
            _authorization = authorization;
        }

        /// <summary>Not permission-gated — called internally by handlers across the app to record the audit trail for the current user's own action, not a user-facing read/write of the Audit module.</summary>
        public async Task LogActionAsync(string entityName, int entityId, string action, string? details = null)
        {
            await _mediator.Send(new LogActionCommand(entityName, entityId, action, details));
        }

        public async Task<List<HotelPOS.Application.DTOs.Audit.AuditLogDto>> GetLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            _authorization.EnsurePermission(PermissionModules.Audit);
            return await _mediator.Send(new GetLogsQuery(from, to));
        }
    }
}
