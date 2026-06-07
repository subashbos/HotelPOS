using MediatR;

namespace HotelPOS.Domain.Events
{
    public class EntityActionEvent : INotification
    {
        public string EntityName { get; }
        public int EntityId { get; }
        public string Action { get; }
        public string? Details { get; }

        public EntityActionEvent(string entityName, int entityId, string action, string? details = null)
        {
            EntityName = entityName;
            EntityId = entityId;
            Action = action;
            Details = details;
        }
    }
}
