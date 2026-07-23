using HotelPOS.Domain.Common.Constants;
using FluentAssertions;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using Moq;
using Xunit;

namespace HotelPOS.Tests;

public class VoidOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_CallsOrderServiceVoidOrderInternalAsync()
    {
        // Arrange
        var orderServiceMock = new Mock<IOrderService>();
        var handler = new VoidOrderCommandHandler(orderServiceMock.Object);
        var command = new VoidOrderCommand(OrderId: 1, Reason: "Mistake", AuthorizedUser: RoleNames.Admin);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        orderServiceMock.Verify(s => s.VoidOrderInternalAsync(1, "Mistake", RoleNames.Admin), Times.Once);
    }
}
