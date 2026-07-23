using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using System.Threading;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class PurchaseServiceTests
    {
        [Fact]
        public async Task SavePurchaseAsync_MediatorPath_PublishesCreateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new PurchaseService(mediatorMock.Object);

            var purchase = new Purchase
            {
                Id = 15,
                SupplierId = 3,
                InvoiceNumber = "INV-100",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, ItemName = "Milk", Quantity = 5, UnitPrice = 40 }
                }
            };

            await service.SavePurchaseAsync(purchase);

            mediatorMock.Verify(m => m.Send(It.IsAny<SavePurchaseCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Purchase" && e.EntityId == 15 && e.Action == "Create"), default),
                Times.Once);
        }
    }
}
