using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Application.UseCases.Purchases.Queries;
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

        [Fact]
        public async Task GetPurchaseByIdAsync_MediatorPath_SendsQuery()
        {
            var mediatorMock = new Mock<IMediator>();
            var expected = new Purchase { Id = 7 };
            mediatorMock.Setup(m => m.Send(It.Is<GetPurchaseByIdQuery>(q => q.Id == 7), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var service = new PurchaseService(mediatorMock.Object);

            var result = await service.GetPurchaseByIdAsync(7);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task UpdatePurchaseAsync_MediatorPath_PublishesUpdateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new PurchaseService(mediatorMock.Object);

            var purchase = new Purchase
            {
                Id = 20,
                SupplierId = 3,
                InvoiceNumber = "INV-200",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, ItemName = "Milk", Quantity = 5, UnitPrice = 40 }
                }
            };

            await service.UpdatePurchaseAsync(purchase);

            mediatorMock.Verify(m => m.Send(It.Is<UpdatePurchaseCommand>(c => c.Purchase == purchase), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Purchase" && e.EntityId == 20 && e.Action == "Update"), default),
                Times.Once);
        }

        [Fact]
        public async Task DeletePurchaseAsync_MediatorPath_PublishesDeleteEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new PurchaseService(mediatorMock.Object);

            await service.DeletePurchaseAsync(9);

            mediatorMock.Verify(m => m.Send(It.Is<DeletePurchaseCommand>(c => c.Id == 9), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Purchase" && e.EntityId == 9 && e.Action == "Delete"), default),
                Times.Once);
        }
    }
}
