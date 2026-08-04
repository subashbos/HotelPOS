using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.Reservations.Queries
{
    public record GetReservationByIdQuery(int Id) : IRequest<Reservation?>;

    public class GetReservationByIdQueryHandler : IRequestHandler<GetReservationByIdQuery, Reservation?>
    {
        private readonly IReservationRepository _repository;

        public GetReservationByIdQueryHandler(IReservationRepository repository)
        {
            _repository = repository;
        }

        public async Task<Reservation?> Handle(GetReservationByIdQuery request, CancellationToken cancellationToken)
        {
            return await _repository.GetByIdAsync(request.Id);
        }
    }
}
