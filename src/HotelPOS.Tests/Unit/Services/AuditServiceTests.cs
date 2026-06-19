using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotelPOS.Application.DTOs.Audit;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Audit.Commands;
using HotelPOS.Application.UseCases.Audit.Queries;
using HotelPOS.Domain.Entities;
using MediatR;
using Moq;
using Xunit;
using AutoMapper;

namespace HotelPOS.Tests
{
    public class AuditServiceTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly AuditService _service;

        public AuditServiceTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _service = new AuditService(_mediatorMock.Object);
        }

        [Fact]
        public async Task LogActionAsync_Should_Send_Command()
        {
            await _service.LogActionAsync("Item", 123, "Update", "Price changed");
            _mediatorMock.Verify(m => m.Send(It.Is<LogActionCommand>(c => c.EntityName == "Item" && c.EntityId == 123 && c.Action == "Update" && c.Details == "Price changed"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetLogsAsync_Should_Send_Query()
        {
            var logs = new List<AuditLogDto> { new AuditLogDto { Action = "Test" } };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetLogsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(logs);

            var result = await _service.GetLogsAsync();
            Assert.Equal(logs, result);
        }
    }

    public class LogActionCommandHandlerTests
    {
        private readonly Mock<IAuditRepository> _repoMock;
        private readonly Mock<IUserContext> _userContextMock;
        private readonly LogActionCommandHandler _handler;

        public LogActionCommandHandlerTests()
        {
            _repoMock = new Mock<IAuditRepository>();
            _userContextMock = new Mock<IUserContext>();
            _handler = new LogActionCommandHandler(_repoMock.Object, _userContextMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Populate_Log_Correctly()
        {
            _userContextMock.Setup(u => u.CurrentUsername).Returns("testuser");
            AuditLog? capturedLog = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .Callback<AuditLog>(log => capturedLog = log)
                     .Returns(Task.CompletedTask);

            await _handler.Handle(new LogActionCommand("Item", 123, "Update", "Price changed"), CancellationToken.None);

            Assert.NotNull(capturedLog);
            Assert.Equal("Item", capturedLog.EntityName);
            Assert.Equal(123, capturedLog.EntityId);
            Assert.Equal("Update", capturedLog.Action);
            Assert.Equal("Price changed", capturedLog.Details);
            Assert.Equal("testuser", capturedLog.Username);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_NullOrWhitespaceUsername_DefaultsToSystem(string? username)
        {
            _userContextMock.Setup(u => u.CurrentUsername).Returns(username!);
            AuditLog? capturedLog = null;
            _repoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>()))
                     .Callback<AuditLog>(log => capturedLog = log)
                     .Returns(Task.CompletedTask);

            await _handler.Handle(new LogActionCommand("Item", 1, "Create"), CancellationToken.None);
            Assert.Equal("System", capturedLog!.Username);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Handle_InvalidEntityName_ThrowsArgumentException(string? invalidEntity)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(new LogActionCommand(invalidEntity!, 1, "Create"), CancellationToken.None));
        }
    }
}
