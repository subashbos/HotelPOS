#nullable enable

using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Reservations.Commands;
using HotelPOS.Application.UseCases.Reservations.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Domain.Events;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class ReservationService : IReservationService
    {
        private readonly IMediator? _mediator;
        private readonly IReservationRepository? _reservationRepository;
        private readonly ITableRepository? _tableRepository;
        private readonly IAuthorizationService? _authorization;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public ReservationService(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Legacy constructor for unit tests that inject repositories directly.</summary>
        public ReservationService(IReservationRepository reservationRepository, ITableRepository tableRepository, IAuthorizationService authorization)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _authorization = authorization;
        }

        public async Task<List<Reservation>> GetReservationsAsync(DateTime? date = null)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetAllReservationsQuery(date));

            return await _reservationRepository!.GetReservationsAsync(date);
        }

        public async Task<Reservation?> GetReservationByIdAsync(int id)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetReservationByIdQuery(id));

            return await _reservationRepository!.GetByIdAsync(id);
        }

        public async Task SaveReservationAsync(Reservation reservation)
        {
            if (reservation == null) throw new ArgumentNullException(nameof(reservation));

            if (_mediator != null)
            {
                await _mediator.Send(new SaveReservationCommand(reservation));
                await _mediator.Publish(new EntityActionEvent("Reservation", reservation.Id, "Create",
                    $"Table: {reservation.TableId}, Date: {reservation.ReservationDate:d}, Party: {reservation.PartySize}"));
                return;
            }

            var handler = new SaveReservationCommandHandler(_reservationRepository!, _tableRepository!, _authorization!);
            await handler.Handle(new SaveReservationCommand(reservation), CancellationToken.None);
        }

        public async Task UpdateReservationAsync(Reservation reservation)
        {
            if (reservation == null) throw new ArgumentNullException(nameof(reservation));

            if (_mediator != null)
            {
                await _mediator.Send(new UpdateReservationCommand(reservation));
                await _mediator.Publish(new EntityActionEvent("Reservation", reservation.Id, "Update",
                    $"Table: {reservation.TableId}, Date: {reservation.ReservationDate:d}, Party: {reservation.PartySize}"));
                return;
            }

            var handler = new UpdateReservationCommandHandler(_reservationRepository!, _tableRepository!, _authorization!);
            await handler.Handle(new UpdateReservationCommand(reservation), CancellationToken.None);
        }

        public async Task DeleteReservationAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteReservationCommand(id));
                await _mediator.Publish(new EntityActionEvent("Reservation", id, "Delete", "Deleted"));
                return;
            }

            var handler = new DeleteReservationCommandHandler(_reservationRepository!, _authorization!);
            await handler.Handle(new DeleteReservationCommand(id), CancellationToken.None);
        }

        public async Task ChangeStatusAsync(int id, string newStatus)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new ChangeReservationStatusCommand(id, newStatus));
                await _mediator.Publish(new EntityActionEvent("Reservation", id, "ChangeStatus", newStatus));
                return;
            }

            var handler = new ChangeReservationStatusCommandHandler(_reservationRepository!, _authorization!);
            await handler.Handle(new ChangeReservationStatusCommand(id, newStatus), CancellationToken.None);
        }
    }
}
