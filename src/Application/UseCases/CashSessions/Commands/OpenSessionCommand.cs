using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases.CashSessions.Commands
{
    public record OpenSessionCommand(OpenSessionDto Dto) : IRequest<int>;

    public class OpenSessionCommandHandler : IRequestHandler<OpenSessionCommand, int>
    {
        private readonly ICashRepository _repository;

        public OpenSessionCommandHandler(ICashRepository repository)
        {
            _repository = repository;
        }

        public async Task<int> Handle(OpenSessionCommand request, CancellationToken cancellationToken)
        {
            var active = await _repository.GetCurrentSessionAsync();
            if (active != null)
                throw new InvalidOperationException("A session is already open.");

            var session = new CashSession
            {
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = request.Dto.OpeningBalance,
                OpenedBy = request.Dto.OpenedBy,
                Status = CashSessionStatuses.Open
            };

            await _repository.AddAsync(session);
            return session.Id;
        }
    }
}
