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

        [Fact]
        public async Task ChangeStatusAsync_LegacyPath_InvalidStatusString_ThrowsArgumentException()
        {
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ChangeStatusAsync(1, "NotAStatus"));
            Assert.Contains("not a valid reservation status", ex.Message);
        }

        [Fact]
        public async Task ChangeStatusAsync_LegacyPath_NotFound_ThrowsKeyNotFoundException()
        {
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            reservationRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Reservation?)null);
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ChangeStatusAsync(99, ReservationStatuses.CheckedIn));
        }

        [Fact]
        public async Task ChangeStatusAsync_LegacyPath_InvalidTransition_ThrowsInvalidOperationException()
        {
            // A Completed reservation has no valid forward transitions (NextStatuses[Completed] is empty).
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            var reservation = new Reservation { Id = 5, Status = ReservationStatuses.Completed };
            reservationRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(reservation);
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ChangeStatusAsync(5, ReservationStatuses.CheckedIn));
            Assert.Contains("Cannot move a Completed reservation to CheckedIn", ex.Message);
            reservationRepoMock.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SaveReservationAsync_LegacyPath_TableNotFound_ThrowsArgumentException()
        {
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            tableRepoMock.Setup(t => t.GetByIdAsync(3)).ReturnsAsync((Table?)null);
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            var reservation = new Reservation { TableId = 3, PartySize = 2, ReservationDate = DateTime.Today };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveReservationAsync(reservation));
            Assert.Contains("does not exist", ex.Message);
        }

        [Fact]
        public async Task SaveReservationAsync_LegacyPath_PartySizeExceedsCapacity_ThrowsArgumentException()
        {
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            tableRepoMock.Setup(t => t.GetByIdAsync(3)).ReturnsAsync(new Table { Id = 3, Name = "T3", Capacity = 4 });
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            var reservation = new Reservation { TableId = 3, PartySize = 6, ReservationDate = DateTime.Today };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.SaveReservationAsync(reservation));
            Assert.Contains("exceeds table", ex.Message);
        }

        [Fact]
        public async Task SaveReservationAsync_LegacyPath_OverlappingTimeSlot_ThrowsInvalidOperationException()
        {
            var reservationRepoMock = new Mock<IReservationRepository>();
            var tableRepoMock = new Mock<ITableRepository>();
            var date = DateTime.Today;

            tableRepoMock.Setup(t => t.GetByIdAsync(3)).ReturnsAsync(new Table { Id = 3, Name = "T3", Capacity = 4 });
            reservationRepoMock.Setup(r => r.BeginTransactionAsync()).Returns(Task.CompletedTask);
            reservationRepoMock.Setup(r => r.AcquireTableDateLockAsync(3, date.Date)).Returns(Task.CompletedTask);
            reservationRepoMock.Setup(r => r.RollbackTransactionAsync()).Returns(Task.CompletedTask);
            reservationRepoMock
                .Setup(r => r.GetActiveReservationsForTableAsync(3, date.Date, null))
                .ReturnsAsync(new List<Reservation>
                {
                    new Reservation { TableId = 3, ReservationDate = date, StartTime = TimeSpan.FromHours(18), EndTime = TimeSpan.FromHours(20) }
                });
            var service = new ReservationService(reservationRepoMock.Object, tableRepoMock.Object, TestAuthorization.AllowAll().Object);

            var reservation = new Reservation
            {
                TableId = 3,
                PartySize = 2,
                ReservationDate = date,
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(21)
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveReservationAsync(reservation));
            Assert.Contains("already booked", ex.Message);
            reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
            reservationRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
        }
    }
}
