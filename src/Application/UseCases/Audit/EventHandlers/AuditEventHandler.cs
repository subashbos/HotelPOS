using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases.Audit.EventHandlers
{
    public class AuditEventHandler : INotificationHandler<EntityActionEvent>
    {
        private readonly IAuditService _auditService;

        public AuditEventHandler(IAuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task Handle(EntityActionEvent notification, CancellationToken cancellationToken)
        {
            await _auditService.LogActionAsync(
                notification.EntityName,
                notification.EntityId,
                notification.Action,
                notification.Details);
        }
    }
}
