using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SaveReservationCommandHandlerTests
    {
        private readonly Mock<IReservationRepository> _reservationRepoMock = new();
        private readonly Mock<ITableRepository> _tableRepoMock = new();
        private readonly SaveReservationCommandHandler _handler;

        public SaveReservationCommandHandlerTests()
        {
            _handler = new SaveReservationCommandHandler(_reservationRepoMock.Object, _tableRepoMock.Object, TestAuthorization.AllowAll().Object);
            _tableRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Table { Id = 1, Name = "T1", Capacity = 4 });
            _reservationRepoMock.Setup(r => r.GetActiveReservationsForTableAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<Reservation>());
        }

        [Fact]
        public async Task Handle_NoConflict_AddsAndStampsReservedStatus()
        {
            var reservation = new Reservation
            {
                TableId = 1,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(20)
            };

            await _handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None);

            Assert.Equal("Reserved", reservation.Status);
            _reservationRepoMock.Verify(r => r.AddAsync(reservation), Times.Once);
        }

        [Fact]
        public async Task Handle_PartySizeExceedsCapacity_ThrowsAndDoesNotAdd()
        {
            var reservation = new Reservation
            {
                TableId = 1,
                PartySize = 10,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(20)
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownTable_ThrowsArgumentException()
        {
            var reservation = new Reservation
            {
                TableId = 999,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19),
                EndTime = TimeSpan.FromHours(20)
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_OverlappingTimeSlot_ThrowsInvalidOperationAndDoesNotAdd()
        {
            _reservationRepoMock.Setup(r => r.GetActiveReservationsForTableAsync(1, new DateTime(2026, 8, 10), null))
                .ReturnsAsync(new List<Reservation>
                {
                    new Reservation { TableId = 1, StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(20) }
                });

            var reservation = new Reservation
            {
                TableId = 1,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(19.5),
                EndTime = TimeSpan.FromHours(21)
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AdjacentNonOverlappingTimeSlot_Succeeds()
        {
            // Existing: 19:00-20:00. New: 20:00-21:00 - back-to-back, not overlapping.
            _reservationRepoMock.Setup(r => r.GetActiveReservationsForTableAsync(1, new DateTime(2026, 8, 10), null))
                .ReturnsAsync(new List<Reservation>
                {
                    new Reservation { TableId = 1, StartTime = TimeSpan.FromHours(19), EndTime = TimeSpan.FromHours(20) }
                });

            var reservation = new Reservation
            {
                TableId = 1,
                PartySize = 2,
                ReservationDate = new DateTime(2026, 8, 10),
                StartTime = TimeSpan.FromHours(20),
                EndTime = TimeSpan.FromHours(21)
            };

            var exception = await Record.ExceptionAsync(
                () => _handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None));

            Assert.Null(exception);
            _reservationRepoMock.Verify(r => r.AddAsync(reservation), Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutPermission_ThrowsAndDoesNotAdd()
        {
            var handler = new SaveReservationCommandHandler(_reservationRepoMock.Object, _tableRepoMock.Object, TestAuthorization.DenyAll().Object);
            var reservation = new Reservation { TableId = 1, PartySize = 2 };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
        }
    }
}
