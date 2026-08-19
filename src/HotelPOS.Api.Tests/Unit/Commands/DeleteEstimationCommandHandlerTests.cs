using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class DeleteEstimationCommandHandlerTests
    {
        private readonly Mock<IEstimationRepository> _estimationRepoMock = new();
        private readonly DeleteEstimationCommandHandler _handler;

        public DeleteEstimationCommandHandlerTests()
        {
            _handler = new DeleteEstimationCommandHandler(_estimationRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_DraftEstimation_DeletesViaRepository()
        {
            var estimation = new Estimation { Id = 1, Status = EstimationStatuses.Draft };
            _estimationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(estimation);

            await _handler.Handle(new DeleteEstimationCommand(1), CancellationToken.None);

            _estimationRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task Handle_ConvertedEstimation_ThrowsAndDoesNotDelete()
        {
            var estimation = new Estimation { Id = 2, Status = EstimationStatuses.Converted, ConvertedOrderId = 42 };
            _estimationRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(estimation);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new DeleteEstimationCommand(2), CancellationToken.None));

            _estimationRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownEstimation_IsIdempotentAndDoesNotThrow()
        {
            _estimationRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Estimation?)null);

            var ex = await Record.ExceptionAsync(
                () => _handler.Handle(new DeleteEstimationCommand(404), CancellationToken.None));

            Assert.Null(ex);
            _estimationRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithoutPermission_ThrowsAndDoesNotDelete()
        {
            var handler = new DeleteEstimationCommandHandler(_estimationRepoMock.Object, TestAuthorization.DenyAll().Object);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => handler.Handle(new DeleteEstimationCommand(1), CancellationToken.None));

            _estimationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _estimationRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
