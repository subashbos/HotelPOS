using FluentAssertions;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class RefundOrderCommandHandlerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly RefundOrderCommandHandler _handler;

    public RefundOrderCommandHandlerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _handler = new RefundOrderCommandHandler(_orderServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToOrderServiceRefundOrderInternalAsync()
    {
        var items = new List<OrderItemRefundDto> { new(1, 2) };
        var command = new RefundOrderCommand(OrderId: 5, ItemsToRefund: items, Reason: "Customer complaint");

        await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(s => s.RefundOrderInternalAsync(5, items, "Customer complaint"), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesExceptionsFromOrderService()
    {
        var items = new List<OrderItemRefundDto> { new(1, 2) };
        _orderServiceMock.Setup(s => s.RefundOrderInternalAsync(5, items, "Reason"))
            .ThrowsAsync(new InvalidOperationException("Cannot refund a void order."));
        var command = new RefundOrderCommand(OrderId: 5, ItemsToRefund: items, Reason: "Reason");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot refund a void order.");
    }
}
