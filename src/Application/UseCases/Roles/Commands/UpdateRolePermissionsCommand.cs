using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Roles.Commands
{
    public record UpdateRolePermissionsCommand(int RoleId, List<RolePermission> Permissions) : IRequest;

    public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand>
    {
        private readonly IRoleRepository _roleRepository;

        public UpdateRolePermissionsCommandHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
        {
            await _roleRepository.UpdatePermissionsAsync(request.RoleId, request.Permissions);
        }
    }
}
