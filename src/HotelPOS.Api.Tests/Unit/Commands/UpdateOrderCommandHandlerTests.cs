using FluentAssertions;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class UpdateOrderCommandHandlerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly UpdateOrderCommandHandler _handler;

    public UpdateOrderCommandHandlerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _handler = new UpdateOrderCommandHandler(_orderServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DelegatesToOrderServiceUpdateOrderInternalAsync()
    {
        var order = new Order { Id = 1, Items = new List<OrderItem> { new OrderItem { ItemId = 1, Quantity = 2 } } };
        var command = new UpdateOrderCommand(order);

        await _handler.Handle(command, CancellationToken.None);

        _orderServiceMock.Verify(s => s.UpdateOrderInternalAsync(order), Times.Once);
    }

    [Fact]
    public async Task Handle_PropagatesExceptionsFromOrderService()
    {
        var order = new Order { Id = 1 };
        _orderServiceMock.Setup(s => s.UpdateOrderInternalAsync(order))
            .ThrowsAsync(new KeyNotFoundException("Order #1 not found."));
        var command = new UpdateOrderCommand(order);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Order #1 not found.");
    }
}
