using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Roles.Queries
{
    public record GetRoleByIdQuery(int Id) : IRequest<Role?>;

    public class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, Role?>
    {
        private readonly IRoleRepository _roleRepository;

        public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Role?> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            return await _roleRepository.GetRoleByIdAsync(request.Id);
        }
    }
}
