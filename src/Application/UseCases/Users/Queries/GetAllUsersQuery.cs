using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Users.Queries
{
    public record GetAllUsersQuery() : IRequest<List<User>>;

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<User>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<User>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            return await _userRepository.GetAllAsync();
        }
    }
}
