using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    /// <summary>
    /// PurchaseService's legacy repository-direct constructor is covered by
    /// HotelPOS.Tests/Integration/PurchaseTests.cs. This file covers the mediator (production/DI)
    /// constructor path — API and WPF both always resolve PurchaseService via IMediator
    /// (see Program.cs/App.Services.cs) — specifically the audit event publishing added on top of it.
    /// </summary>
    public class PurchaseServiceTests
    {
        [Fact]
        public async Task SavePurchaseAsync_ViaMediator_PublishesCreateAuditEvent()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<SavePurchaseCommand>(), default))
                .Callback<SavePurchaseCommand, System.Threading.CancellationToken>((cmd, _) => cmd.Purchase.Id = 42)
                .Returns(Task.CompletedTask);

            var service = new PurchaseService(mediator.Object);
            var purchase = new Purchase { SupplierId = 3, InvoiceNumber = "INV-1001", GrandTotal = 999.50m };

            await service.SavePurchaseAsync(purchase);

            mediator.Verify(m => m.Publish(It.Is<EntityActionEvent>(e =>
                e.EntityName == "Purchase" && e.EntityId == 42 && e.Action == "Create"), default), Times.Once);
        }

        [Fact]
        public async Task SavePurchaseAsync_NullPurchase_ThrowsWithoutPublishing()
        {
            var mediator = new Mock<IMediator>();
            var service = new PurchaseService(mediator.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.SavePurchaseAsync(null!));

            mediator.Verify(m => m.Publish(It.IsAny<EntityActionEvent>(), default), Times.Never);
        }
    }
}
