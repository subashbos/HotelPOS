using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Queries
{
    public record GetAllReservationsQuery(DateTime? Date = null) : IRequest<List<Reservation>>;

    public class GetAllReservationsQueryHandler : IRequestHandler<GetAllReservationsQuery, List<Reservation>>
    {
        private readonly IReservationRepository _repository;

        public GetAllReservationsQueryHandler(IReservationRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Reservation>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetReservationsAsync(request.Date);
        }
    }
}
