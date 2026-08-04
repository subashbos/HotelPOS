using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ChangeReservationStatusCommandHandlerTests
    {
        private readonly Mock<IReservationRepository> _reservationRepoMock = new();
        private readonly ChangeReservationStatusCommandHandler _handler;

        public ChangeReservationStatusCommandHandlerTests()
        {
            _handler = new ChangeReservationStatusCommandHandler(_reservationRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_ReservedToCheckedIn_UpdatesStatus()
        {
            _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Reservation { Id = 1, Status = ReservationStatuses.Reserved });

            await _handler.Handle(new ChangeReservationStatusCommand(1, ReservationStatuses.CheckedIn), CancellationToken.None);

            _reservationRepoMock.Verify(r => r.UpdateStatusAsync(1, ReservationStatuses.CheckedIn), Times.Once);
        }

        [Fact]
        public async Task Handle_ReservedDirectlyToCompleted_ThrowsInvalidOperation()
        {
            _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Reservation { Id = 1, Status = ReservationStatuses.Reserved });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new ChangeReservationStatusCommand(1, ReservationStatuses.Completed), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AlreadyCancelled_RejectsAnyFurtherTransition()
        {
            _reservationRepoMock.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Reservation { Id = 1, Status = ReservationStatuses.Cancelled });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new ChangeReservationStatusCommand(1, ReservationStatuses.CheckedIn), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnknownStatusValue_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _handler.Handle(new ChangeReservationStatusCommand(1, "NotARealStatus"), CancellationToken.None));

            _reservationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownReservation_ThrowsKeyNotFound()
        {
            _reservationRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Reservation?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(new ChangeReservationStatusCommand(404, ReservationStatuses.CheckedIn), CancellationToken.None));
        }
    }
}
