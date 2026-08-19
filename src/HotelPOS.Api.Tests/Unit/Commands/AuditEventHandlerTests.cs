using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Audit.EventHandlers;
using HotelPOS.Domain.Events;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class AuditEventHandlerTests
    {
        private readonly Mock<IAuditService> _auditService = new();
        private readonly AuditEventHandler _handler;

        public AuditEventHandlerTests()
        {
            _handler = new AuditEventHandler(_auditService.Object);
        }

        [Fact]
        public async Task Handle_ForwardsEventFieldsToAuditService()
        {
            var notification = new EntityActionEvent("Order", 42, "Create", "Table: 5, Total: 1500");

            await _handler.Handle(notification, CancellationToken.None);

            _auditService.Verify(a => a.LogActionAsync("Order", 42, "Create", "Table: 5, Total: 1500"), Times.Once);
        }

        [Fact]
        public async Task Handle_NullDetails_ForwardedAsNull()
        {
            var notification = new EntityActionEvent("Expense", 7, "Delete");

            await _handler.Handle(notification, CancellationToken.None);

            _auditService.Verify(a => a.LogActionAsync("Expense", 7, "Delete", null), Times.Once);
        }
    }
}
