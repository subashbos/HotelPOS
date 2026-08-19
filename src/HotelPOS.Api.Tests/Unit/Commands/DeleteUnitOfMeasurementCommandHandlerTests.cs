using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class DeleteUnitOfMeasurementCommandHandlerTests
    {
        private readonly Mock<IUnitOfMeasurementRepository> _repo = new();
        private readonly Mock<IItemRepository> _itemRepo = new();
        private readonly DeleteUnitOfMeasurementCommandHandler _handler;

        public DeleteUnitOfMeasurementCommandHandlerTests()
        {
            _handler = new DeleteUnitOfMeasurementCommandHandler(_repo.Object, _itemRepo.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_UnitInUseByItem_ThrowsAndDoesNotDelete()
        {
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>
            {
                new() { Id = 1, UnitId = 5 }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(new DeleteUnitOfMeasurementCommand(5), CancellationToken.None));

            _repo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnitNotInUse_Deletes()
        {
            _itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>
            {
                new() { Id = 1, UnitId = 6 }
            });

            await _handler.Handle(new DeleteUnitOfMeasurementCommand(5), CancellationToken.None);

            _repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }
    }
}
