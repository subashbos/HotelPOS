using FluentAssertions;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class ProcessPartialPaymentCommandHandlerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly ProcessPartialPaymentCommandHandler _handler;

    public ProcessPartialPaymentCommandHandlerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _handler = new ProcessPartialPaymentCommandHandler(_orderServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToOrderServiceProcessPartialPaymentInternalAsync()
    {
        var command = new ProcessPartialPaymentCommand(OrderId: 1, Cash: 200m, Card: 100m, Upi: 0m);

        await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(s => s.ProcessPartialPaymentInternalAsync(1, 200m, 100m, 0m), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesExceptionsFromOrderService()
    {
        _orderServiceMock.Setup(s => s.ProcessPartialPaymentInternalAsync(1, 100m, 0m, 0m))
            .ThrowsAsync(new KeyNotFoundException("Order #1 not found."));
        var command = new ProcessPartialPaymentCommand(OrderId: 1, Cash: 100m, Card: 0m, Upi: 0m);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Order #1 not found.");
    }
}
