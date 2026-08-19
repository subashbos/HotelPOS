using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.UnitOfMeasurements.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class CreateUnitOfMeasurementCommandHandlerTests
    {
        private readonly Mock<IUnitOfMeasurementRepository> _repo = new();
        private readonly CreateUnitOfMeasurementCommandHandler _handler;

        public CreateUnitOfMeasurementCommandHandlerTests()
        {
            _handler = new CreateUnitOfMeasurementCommandHandler(_repo.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_DuplicateNameCaseInsensitive_Throws()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>
            {
                new() { Id = 1, Name = "Kilogram" }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _handler.Handle(new CreateUnitOfMeasurementCommand("kilogram"), CancellationToken.None));

            _repo.Verify(r => r.AddAsync(It.IsAny<UnitOfMeasurement>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NewName_TrimsAndAdds()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>());
            _repo.Setup(r => r.AddAsync(It.IsAny<UnitOfMeasurement>()))
                .ReturnsAsync((UnitOfMeasurement u) => { u.Id = 7; return u; });

            var id = await _handler.Handle(new CreateUnitOfMeasurementCommand("  Litre  ", 2), CancellationToken.None);

            Assert.Equal(7, id);
            _repo.Verify(r => r.AddAsync(It.Is<UnitOfMeasurement>(u => u.Name == "Litre" && u.DisplayOrder == 2)), Times.Once);
        }
    }
}
