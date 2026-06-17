using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.Interfaces;
using MediatR;

namespace HotelPOS.Application.UseCases.CashSessions.Commands
{
    public record CloseSessionCommand(CloseSessionDto Dto) : IRequest;

    public class CloseSessionCommandHandler : IRequestHandler<CloseSessionCommand>
    {
        private readonly ICashRepository _repository;

        public CloseSessionCommandHandler(ICashRepository repository)
        {
            _repository = repository;
        }

        public async Task Handle(CloseSessionCommand request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetCurrentSessionAsync()
                ?? throw new InvalidOperationException("No active session to close.");

            var sales = await _repository.GetSalesTotalAsync(session.OpenedAt);

            session.ClosedAt = DateTime.UtcNow;
            session.ClosedBy = request.Dto.ClosedBy;
            session.ClosingBalance = session.OpeningBalance + sales;
            session.ActualCash = request.Dto.ActualCash;
            session.Notes = request.Dto.Notes;
            session.Status = "Closed";

            await _repository.UpdateAsync(session);
        }
    }
}
