using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class UpdateUnitOfMeasurementCommandHandlerTests
    {
        private readonly Mock<IUnitOfMeasurementRepository> _repo = new();
        private readonly UpdateUnitOfMeasurementCommandHandler _handler;

        public UpdateUnitOfMeasurementCommandHandlerTests()
        {
            _handler = new UpdateUnitOfMeasurementCommandHandler(_repo.Object);
        }

        [Fact]
        public async Task Handle_DuplicateNameOnAnotherUnit_Throws()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>
            {
                new() { Id = 1, Name = "Kilogram" },
                new() { Id = 2, Name = "Gram" }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(new UpdateUnitOfMeasurementCommand(2, "kilogram"), CancellationToken.None));

            _repo.Verify(r => r.UpdateAsync(It.IsAny<UnitOfMeasurement>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SameNameOnSameUnit_DoesNotThrow()
        {
            var existing = new UnitOfMeasurement { Id = 1, Name = "Kilogram", DisplayOrder = 1 };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement> { existing });
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _handler.Handle(new UpdateUnitOfMeasurementCommand(1, "Kilogram", 3), CancellationToken.None);

            Assert.Equal(3, existing.DisplayOrder);
            _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task Handle_UnitNotFound_Throws()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>());
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UnitOfMeasurement?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _handler.Handle(new UpdateUnitOfMeasurementCommand(99, "Litre"), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ValidUpdate_TrimsNameAndPersists()
        {
            var existing = new UnitOfMeasurement { Id = 1, Name = "Old", DisplayOrder = 1 };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement> { existing });
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _handler.Handle(new UpdateUnitOfMeasurementCommand(1, "  New Name  ", 5), CancellationToken.None);

            Assert.Equal("New Name", existing.Name);
            Assert.Equal(5, existing.DisplayOrder);
            _repo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }
    }
}
