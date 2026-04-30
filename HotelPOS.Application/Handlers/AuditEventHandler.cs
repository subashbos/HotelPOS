using HotelPOS.Application.Interface;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.Handlers
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
