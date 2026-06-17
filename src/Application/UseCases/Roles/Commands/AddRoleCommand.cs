using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Roles.Commands
{
    public record AddRoleCommand(string Name, string Description) : IRequest<bool>;

    public class AddRoleCommandHandler : IRequestHandler<AddRoleCommand, bool>
    {
        private readonly IRoleRepository _roleRepository;

        private static readonly string[] DefaultModules =
        {
            "Dashboard", "Billing", "Items", "Categories", "Tables",
            "Ledger", "Journal", "Settings", "Audit", "Shift", "Roles", "SalesReport"
        };

        public AddRoleCommandHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<bool> Handle(AddRoleCommand request, CancellationToken cancellationToken)
        {
            var existing = await _roleRepository.GetRoleByNameAsync(request.Name);
            if (existing != null) return false;

            var role = new Role { Name = request.Name.Trim(), Description = request.Description };

            foreach (var mod in DefaultModules)
                role.Permissions.Add(new RolePermission { ModuleName = mod, CanAccess = false });

            await _roleRepository.AddRoleAsync(role);
            return true;
        }
    }
}
