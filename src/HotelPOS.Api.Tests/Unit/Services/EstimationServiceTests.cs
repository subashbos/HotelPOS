using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Application.UseCases.Estimations.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using System.Threading;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class EstimationServiceTests
    {
        [Fact]
        public async Task SaveEstimationAsync_MediatorPath_PublishesCreateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new EstimationService(mediatorMock.Object);

            var estimation = new Estimation
            {
                Id = 15,
                EstimationNumber = "EST-100",
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Banquet Hall", Quantity = 1, UnitPrice = 5000 }
                }
            };

            await service.SaveEstimationAsync(estimation);

            mediatorMock.Verify(m => m.Send(It.IsAny<SaveEstimationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Estimation" && e.EntityId == 15 && e.Action == "Create"), default),
                Times.Once);
        }

        [Fact]
        public async Task GetEstimationByIdAsync_MediatorPath_SendsQuery()
        {
            var mediatorMock = new Mock<IMediator>();
            var expected = new Estimation { Id = 7 };
            mediatorMock.Setup(m => m.Send(It.Is<GetEstimationByIdQuery>(q => q.Id == 7), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var service = new EstimationService(mediatorMock.Object);

            var result = await service.GetEstimationByIdAsync(7);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task UpdateEstimationAsync_MediatorPath_PublishesUpdateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new EstimationService(mediatorMock.Object);

            var estimation = new Estimation
            {
                Id = 20,
                EstimationNumber = "EST-200",
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Banquet Hall", Quantity = 1, UnitPrice = 5000 }
                }
            };

            await service.UpdateEstimationAsync(estimation);

            mediatorMock.Verify(m => m.Send(It.Is<UpdateEstimationCommand>(c => c.Estimation == estimation), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Estimation" && e.EntityId == 20 && e.Action == "Update"), default),
                Times.Once);
        }

        [Fact]
        public async Task DeleteEstimationAsync_MediatorPath_PublishesDeleteEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new EstimationService(mediatorMock.Object);

            await service.DeleteEstimationAsync(9);

            mediatorMock.Verify(m => m.Send(It.Is<DeleteEstimationCommand>(c => c.Id == 9), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Estimation" && e.EntityId == 9 && e.Action == "Delete"), default),
                Times.Once);
        }

        [Fact]
        public async Task ConvertToOrderAsync_MediatorPath_PublishesConvertEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            mediatorMock.Setup(m => m.Send(It.Is<ConvertEstimationToOrderCommand>(c => c.EstimationId == 11), It.IsAny<CancellationToken>()))
                .ReturnsAsync(88);
            var service = new EstimationService(mediatorMock.Object);

            var orderId = await service.ConvertToOrderAsync(11);

            Assert.Equal(88, orderId);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Estimation" && e.EntityId == 11 && e.Action == "ConvertToOrder"), default),
                Times.Once);
        }
    }
}
