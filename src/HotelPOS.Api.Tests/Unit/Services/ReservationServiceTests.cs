using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Application.UseCases.Reservations.Queries;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;
using Moq;
using System.Threading;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class ReservationServiceTests
    {
        [Fact]
        public async Task SaveReservationAsync_MediatorPath_PublishesCreateEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new ReservationService(mediatorMock.Object);

            var reservation = new Reservation { Id = 15, TableId = 1, PartySize = 2 };

            await service.SaveReservationAsync(reservation);

            mediatorMock.Verify(m => m.Send(It.IsAny<SaveReservationCommand>(), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Reservation" && e.EntityId == 15 && e.Action == "Create"), default),
                Times.Once);
        }

        [Fact]
        public async Task GetReservationByIdAsync_MediatorPath_SendsQuery()
        {
            var mediatorMock = new Mock<IMediator>();
            var expected = new Reservation { Id = 7 };
            mediatorMock.Setup(m => m.Send(It.Is<GetReservationByIdQuery>(q => q.Id == 7), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);
            var service = new ReservationService(mediatorMock.Object);

            var result = await service.GetReservationByIdAsync(7);

            Assert.Same(expected, result);
        }

        [Fact]
        public async Task GetReservationsAsync_MediatorPath_PassesDateFilterThrough()
        {
            var mediatorMock = new Mock<IMediator>();
            var date = new DateTime(2026, 8, 10);
            mediatorMock.Setup(m => m.Send(It.Is<GetAllReservationsQuery>(q => q.Date == date), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Reservation>());
            var service = new ReservationService(mediatorMock.Object);

            await service.GetReservationsAsync(date);

            mediatorMock.Verify(m => m.Send(It.Is<GetAllReservationsQuery>(q => q.Date == date), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteReservationAsync_MediatorPath_PublishesDeleteEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new ReservationService(mediatorMock.Object);

            await service.DeleteReservationAsync(9);

            mediatorMock.Verify(m => m.Send(It.Is<DeleteReservationCommand>(c => c.Id == 9), It.IsAny<CancellationToken>()), Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Reservation" && e.EntityId == 9 && e.Action == "Delete"), default),
                Times.Once);
        }

        [Fact]
        public async Task ChangeStatusAsync_MediatorPath_PublishesChangeStatusEvent()
        {
            var mediatorMock = new Mock<IMediator>();
            var service = new ReservationService(mediatorMock.Object);

            await service.ChangeStatusAsync(11, ReservationStatuses.CheckedIn);

            mediatorMock.Verify(
                m => m.Send(It.Is<ChangeReservationStatusCommand>(c => c.Id == 11 && c.NewStatus == ReservationStatuses.CheckedIn), It.IsAny<CancellationToken>()),
                Times.Once);
            mediatorMock.Verify(
                m => m.Publish(It.Is<EntityActionEvent>(e => e.EntityName == "Reservation" && e.EntityId == 11 && e.Action == "ChangeStatus"), default),
                Times.Once);
        }
    }
}
