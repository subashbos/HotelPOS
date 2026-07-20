using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Orders.Queries
{
    public record GetOrderByIdQuery(int Id) : IRequest<Order?>;

    public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Order?>
    {
        private readonly IOrderRepository _repo;

        public GetOrderByIdQueryHandler(IOrderRepository repo)
        {
            _repo = repo;
        }

        public async Task<Order?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetByIdWithItemsAsync(request.Id);
        }
    }
}
