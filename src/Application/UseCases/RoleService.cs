using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Roles.Commands;
using HotelPOS.Application.UseCases.Roles.Queries;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class RoleService : IRoleService
    {
        private readonly IMediator? _mediator;
        private readonly IRoleRepository? _roleRepository;
        private readonly IAuthorizationService _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public RoleService(IMediator mediator, IAuthorizationService authorization)
        {
            _mediator = mediator;
            _authorization = authorization;
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public RoleService(IRoleRepository roleRepository, IAuthorizationService authorization, bool isTest)
        {
            _roleRepository = roleRepository;
            _authorization = authorization;
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            _authorization.EnsurePermission(PermissionModules.Roles);

            if (_mediator != null)
                return await _mediator.Send(new GetAllRolesQuery());

            return await _roleRepository!.GetAllRolesAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            _authorization.EnsurePermission(PermissionModules.Roles);

            if (_mediator != null)
                return await _mediator.Send(new GetRoleByIdQuery(id));

            return await _roleRepository!.GetRoleByIdAsync(id);
        }

        public async Task<bool> AddRoleAsync(string name, string description)
        {
            _authorization.EnsurePermission(PermissionModules.Roles);

            if (_mediator != null)
                return await _mediator.Send(new AddRoleCommand(name, description));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty.", nameof(name));

            var trimmedName = name.Trim();
            var existing = await _roleRepository!.GetRoleByNameAsync(trimmedName);
            if (existing != null) return false;

            var role = new Role { Name = trimmedName, Description = description };
            foreach (var mod in PermissionModules.All)
                role.Permissions.Add(new RolePermission { ModuleName = mod, CanAccess = false });

            await _roleRepository.AddRoleAsync(role);
            return true;
        }

        public async Task UpdateRolePermissionsAsync(int roleId, List<RolePermission> permissions)
        {
            _authorization.EnsurePermission(PermissionModules.Roles);

            if (_mediator != null)
            {
                await _mediator.Send(new UpdateRolePermissionsCommand(roleId, permissions));
                return;
            }

            await _roleRepository!.UpdatePermissionsAsync(roleId, permissions);
        }

        public async Task DeleteRoleAsync(int id)
        {
            _authorization.EnsurePermission(PermissionModules.Roles);

            if (_mediator != null)
            {
                await _mediator.Send(new DeleteRoleCommand(id));
                return;
            }

            await _roleRepository!.DeleteRoleAsync(id);
        }
    }
}
