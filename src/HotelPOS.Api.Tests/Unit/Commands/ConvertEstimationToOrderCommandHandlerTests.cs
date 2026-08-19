using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Estimations.Commands;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ConvertEstimationToOrderCommandHandlerTests
    {
        private readonly Mock<IEstimationRepository> _estimationRepoMock = new();
        private readonly Mock<IOrderService> _orderServiceMock = new();
        private readonly ConvertEstimationToOrderCommandHandler _handler;

        public ConvertEstimationToOrderCommandHandlerTests()
        {
            _handler = new ConvertEstimationToOrderCommandHandler(
                _estimationRepoMock.Object, _orderServiceMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_AcceptedEstimation_CreatesOrderAndMarksConverted()
        {
            var estimation = new Estimation
            {
                Id = 5,
                Status = EstimationStatuses.Accepted,
                TotalDiscount = 100,
                CustomerId = 3,
                CustomerName = "Acme Corp",
                EstimationItems = new List<EstimationItem>
                {
                    new EstimationItem { ItemId = 1, ItemName = "Banquet Hall", Quantity = 1 }
                }
            };
            _estimationRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(estimation);
            _orderServiceMock.Setup(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>())).ReturnsAsync(42);

            var orderId = await _handler.Handle(new ConvertEstimationToOrderCommand(5), CancellationToken.None);

            Assert.Equal(42, orderId);
            Assert.Equal(EstimationStatuses.Converted, estimation.Status);
            Assert.Equal(42, estimation.ConvertedOrderId);
            _orderServiceMock.Verify(s => s.SaveOrderAsync(It.Is<SaveOrderRequest>(r =>
                r.Discount == 100 && r.CustomerId == 3 && r.CustomerName == "Acme Corp" && r.Items.Count == 1)), Times.Once);
            _estimationRepoMock.Verify(r => r.UpdateAsync(estimation), Times.Once);
        }

        [Fact]
        public async Task Handle_DraftEstimation_ThrowsAndDoesNotCreateOrder()
        {
            var estimation = new Estimation { Id = 6, Status = EstimationStatuses.Draft };
            _estimationRepoMock.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(estimation);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new ConvertEstimationToOrderCommand(6), CancellationToken.None));

            _orderServiceMock.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AlreadyConvertedEstimation_ThrowsAndDoesNotCreateOrder()
        {
            var estimation = new Estimation { Id = 7, Status = EstimationStatuses.Converted, ConvertedOrderId = 99 };
            _estimationRepoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(estimation);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _handler.Handle(new ConvertEstimationToOrderCommand(7), CancellationToken.None));

            _orderServiceMock.Verify(s => s.SaveOrderAsync(It.IsAny<SaveOrderRequest>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownEstimation_ThrowsKeyNotFound()
        {
            _estimationRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Estimation?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(new ConvertEstimationToOrderCommand(404), CancellationToken.None));
        }
    }
}
