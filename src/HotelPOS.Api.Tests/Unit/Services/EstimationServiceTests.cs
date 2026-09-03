using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Application.UseCases.Estimations.Queries;
using HotelPOS.Domain.Common.Constants;
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
                EstimationDate = DateTime.Today,
                GrandTotal = 5000,
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
                EstimationDate = DateTime.Today,
                GrandTotal = 5000,
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

        [Fact]
        public async Task ConvertToOrderAsync_LegacyPath_EstimationNotFound_ThrowsKeyNotFoundException()
        {
            var estimationRepoMock = new Mock<IEstimationRepository>();
            var orderServiceMock = new Mock<IOrderService>();
            estimationRepoMock.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Estimation?)null);
            var service = new EstimationService(estimationRepoMock.Object, orderServiceMock.Object, TestAuthorization.AllowAll().Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ConvertToOrderAsync(42));
        }

        [Fact]
        public async Task ConvertToOrderAsync_LegacyPath_AlreadyConverted_ThrowsInvalidOperationException()
        {
            var estimationRepoMock = new Mock<IEstimationRepository>();
            var orderServiceMock = new Mock<IOrderService>();
            var estimation = new Estimation { Id = 5, Status = EstimationStatuses.Converted, EstimationItems = new List<EstimationItem>() };
            estimationRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(estimation);
            var service = new EstimationService(estimationRepoMock.Object, orderServiceMock.Object, TestAuthorization.AllowAll().Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertToOrderAsync(5));
            Assert.Contains("already been converted", ex.Message);
            orderServiceMock.Verify(o => o.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }

        [Fact]
        public async Task ConvertToOrderAsync_LegacyPath_NotAccepted_ThrowsInvalidOperationException()
        {
            var estimationRepoMock = new Mock<IEstimationRepository>();
            var orderServiceMock = new Mock<IOrderService>();
            var estimation = new Estimation { Id = 6, Status = EstimationStatuses.Draft, EstimationItems = new List<EstimationItem>() };
            estimationRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(estimation);
            var service = new EstimationService(estimationRepoMock.Object, orderServiceMock.Object, TestAuthorization.AllowAll().Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertToOrderAsync(6));
            Assert.Contains("Only an Accepted estimation can be converted", ex.Message);
        }

        [Fact]
        public async Task ConvertToOrderAsync_LegacyPath_Accepted_ConvertsAndReturnsOrderId()
        {
            var estimationRepoMock = new Mock<IEstimationRepository>();
            var orderServiceMock = new Mock<IOrderService>();
            var estimation = new Estimation
            {
                Id = 7,
                Status = EstimationStatuses.Accepted,
                TotalDiscount = 100,
                CustomerName = "Alice",
                CustomerPhone = "12345",
                CustomerId = 3,
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Cake", Quantity = 2, UnitPrice = 500 }
                }
            };
            estimationRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(estimation);
            orderServiceMock.Setup(o => o.SaveOrderAsync(It.IsAny<SaveOrderRequest>())).ReturnsAsync(555);
            estimationRepoMock.Setup(r => r.TryMarkConvertedAsync(7, 555)).ReturnsAsync(true);
            var service = new EstimationService(estimationRepoMock.Object, orderServiceMock.Object, TestAuthorization.AllowAll().Object);

            var orderId = await service.ConvertToOrderAsync(7);

            Assert.Equal(555, orderId);
            orderServiceMock.Verify(
                o => o.SaveOrderAsync(It.Is<SaveOrderRequest>(req => req.Items.Count == 1 && req.CustomerName == "Alice" && req.Discount == 100)),
                Times.Once);
            estimationRepoMock.Verify(r => r.TryMarkConvertedAsync(7, 555), Times.Once);
        }

        [Fact]
        public async Task ConvertToOrderAsync_LegacyPath_LosesConversionRace_VoidsOrderAndThrows()
        {
            // TryMarkConvertedAsync returning false means another request already converted this
            // estimation between the status check and the atomic compare-and-swap - the handler
            // should void the just-created order and surface the same "already converted" error.
            var estimationRepoMock = new Mock<IEstimationRepository>();
            var orderServiceMock = new Mock<IOrderService>();
            var estimation = new Estimation
            {
                Id = 8,
                Status = EstimationStatuses.Accepted,
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Cake", Quantity = 1, UnitPrice = 500 }
                }
            };
            estimationRepoMock.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(estimation);
            orderServiceMock.Setup(o => o.SaveOrderAsync(It.IsAny<SaveOrderRequest>())).ReturnsAsync(777);
            estimationRepoMock.Setup(r => r.TryMarkConvertedAsync(8, 777)).ReturnsAsync(false);
            orderServiceMock.Setup(o => o.DeleteOrderAsync(777)).Returns(Task.CompletedTask);
            var service = new EstimationService(estimationRepoMock.Object, orderServiceMock.Object, TestAuthorization.AllowAll().Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertToOrderAsync(8));

            Assert.Contains("already been converted", ex.Message);
            orderServiceMock.Verify(o => o.DeleteOrderAsync(777), Times.Once);
        }
    }
}
