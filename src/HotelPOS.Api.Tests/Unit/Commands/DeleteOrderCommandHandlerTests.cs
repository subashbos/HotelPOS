using FluentAssertions;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class DeleteOrderCommandHandlerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly DeleteOrderCommandHandler _handler;

    public DeleteOrderCommandHandlerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _handler = new DeleteOrderCommandHandler(_orderServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToOrderServiceDeleteOrderInternalAsync()
    {
        var command = new DeleteOrderCommand(7);

        await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(s => s.DeleteOrderInternalAsync(7), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesExceptionsFromOrderService()
    {
        _orderServiceMock.Setup(s => s.DeleteOrderInternalAsync(7))
            .ThrowsAsync(new InvalidOperationException("Cannot delete."));
        var command = new DeleteOrderCommand(7);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot delete.");
    }
}
