using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Roles.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Commands
{
    public class UpdateRolePermissionsCommandHandlerTests
    {
        private readonly Mock<IRoleRepository> _repo = new();
        private readonly UpdateRolePermissionsCommandHandler _handler;

        public UpdateRolePermissionsCommandHandlerTests()
        {
            _handler = new UpdateRolePermissionsCommandHandler(_repo.Object);
        }

        [Fact]
        public async Task Handle_DelegatesRoleIdAndPermissionsToRepository()
        {
            var permissions = new List<RolePermission>
            {
                new() { ModuleName = "Orders", CanAccess = true },
                new() { ModuleName = "Settings", CanAccess = false }
            };

            await _handler.Handle(new UpdateRolePermissionsCommand(3, permissions), CancellationToken.None);

            _repo.Verify(r => r.UpdatePermissionsAsync(3, permissions), Times.Once);
        }
    }
}
