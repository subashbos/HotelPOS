using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class UpdateReservationCommandHandlerTests
    {
        private readonly Mock<IReservationRepository> _reservationRepoMock = new();
        private readonly Mock<ITableRepository> _tableRepoMock = new();
        private readonly UpdateReservationCommandHandler _handler;

        public UpdateReservationCommandHandlerTests()
        {
            _handler = new UpdateReservationCommandHandler(_reservationRepoMock.Object, _tableRepoMock.Object, TestAuthorization.AllowAll().Object);
            _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Reservation { Id = 1, TableId = 1, Status = ReservationStatuses.Reserved });
            _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Table { Id = 1, Name = "T1", Capacity = 4 });
            _reservationRepoMock.Setup(r => r.GetActiveReservationsForTableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Reservation>());
        }

        [Fact]
        public async Task Handle_NoConflict_LocksTableDateAndCommitsTransaction()
        {
            var reservation = new Reservation
            {
                Id = 1,
                TableId = 1,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(20)
            };

            await _handler.Handle(new UpdateReservationCommand(reservation), CancellationToken.None);

            _reservationRepoMock.Verify(r => r.BeginTransactionAsync(), Times.Once);
            _reservationRepoMock.Verify(r => r.AcquireTableDateLockAsync(1, new DateTime(2026, 8, 10)), Times.Once);
            _reservationRepoMock.Verify(r => r.UpdateAsync(reservation), Times.Once);
            _reservationRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Once);
            _reservationRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_OverlappingTimeSlot_RollsBackTransactionAndDoesNotUpdate()
        {
            _reservationRepoMock.Setup(r => r.GetActiveReservationsForTableAsync(1, new DateTime(2026, 8, 10), 1))
                .ReturnsAsync(new List<Reservation>
                {
                    new Reservation { Id = 2, TableId = 1, StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(20) }
                });

            var reservation = new Reservation
            {
                Id = 1,
                TableId = 1,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19.5),
                EndTime = TimeSpan.FromHours(21)
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new UpdateReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
            _reservationRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _reservationRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_CancelledReservation_ThrowsBeforeStartingTransaction()
        {
            _reservationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Reservation { Id = 1, TableId = 1, Status = ReservationStatuses.Cancelled });
            var reservation = new Reservation { Id = 1, TableId = 1, PartySize = 2 };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new UpdateReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WithoutPermission_ThrowsAndDoesNotUpdate()
        {
            var handler = new UpdateReservationCommandHandler(_reservationRepoMock.Object, _tableRepoMock.Object, TestAuthorization.DenyAll().Object);
            var reservation = new Reservation { Id = 1, TableId = 1, PartySize = 2 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => handler.Handle(new UpdateReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        }
    }
}
