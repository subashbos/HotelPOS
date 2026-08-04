using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SaveEstimationCommandHandlerTests
    {
        private readonly Mock<IEstimationRepository> _estimationRepoMock = new();
        private readonly SaveEstimationCommandHandler _handler;

        public SaveEstimationCommandHandlerTests()
        {
            _handler = new SaveEstimationCommandHandler(_estimationRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_ValidEstimation_AddsViaRepository()
        {
            var estimation = new Estimation
            {
                EstimationNumber = "EST-1",
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Banquet Hall", Quantity = 1, UnitPrice = 5000 }
                }
            };

            await _handler.Handle(new SaveEstimationCommand(estimation), CancellationToken.None);

            _estimationRepoMock.Verify(r => r.AddAsync(estimation), Times.Once);
        }

        [Fact]
        public async Task Handle_WithoutPermission_ThrowsAndDoesNotAdd()
        {
            var handler = new SaveEstimationCommandHandler(_estimationRepoMock.Object, TestAuthorization.DenyAll().Object);
            var estimation = new Estimation { EstimationNumber = "EST-2" };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => handler.Handle(new SaveEstimationCommand(estimation), CancellationToken.None));

            _estimationRepoMock.Verify(r => r.AddAsync(It.IsAny<Estimation>()), Times.Never);
        }
    }
}
