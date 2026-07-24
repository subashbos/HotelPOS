#nullable enable

using HotelPOS.Application.DTOs.CashSession;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.CashSessions.Commands;
using HotelPOS.Application.UseCases.CashSessions.Queries;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;

namespace HotelPOS.Application.UseCases
{
    public class CashService : ICashService
    {
        private readonly IMediator? _mediator;
        private readonly ICashRepository? _repo;

        /// <summary>DI constructor — uses MediatR pipeline (validators + handlers).</summary>
        public CashService(IMediator mediator, ICashRepository repo)
        {
            _mediator = mediator;
            _repo = repo; // also kept for GetSalesTotalAsync which has no separate query
        }

        /// <summary>Legacy constructor for unit tests that inject a repository directly.</summary>
        public CashService(ICashRepository repo)
        {
            _repo = repo;
        }

        public async Task<CashSession?> GetCurrentSessionAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetCurrentSessionQuery());

            return await _repo!.GetCurrentSessionAsync();
        }

        public async Task<int> OpenSessionAsync(decimal openingBalance, string username)
        {
            if (_mediator != null)
                return await _mediator.Send(new OpenSessionCommand(new OpenSessionDto
                {
                    OpeningBalance = openingBalance,
                    OpenedBy = username
                }));

            if (openingBalance < 0)
                throw new ArgumentException("Opening balance cannot be negative.");

            var active = await _repo!.GetCurrentSessionAsync();
            if (active != null)
                throw new InvalidOperationException("A session is already open.");

            var session = new CashSession
            {
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = openingBalance,
                OpenedBy = username,
                Status = CashSessionStatuses.Open
            };
            await _repo.AddAsync(session);
            return session.Id;
        }

        public async Task CloseSessionAsync(decimal actualCash, string? notes, string username)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new CloseSessionCommand(new CloseSessionDto
                {
                    ActualCash = actualCash,
                    Notes = notes,
                    ClosedBy = username
                }));
                return;
            }

            if (actualCash < 0)
                throw new ArgumentException("Actual cash amount cannot be negative.");

            var session = await _repo!.GetCurrentSessionAsync()
                ?? throw new InvalidOperationException("No active session to close.");

            var sales = await _repo.GetSalesTotalAsync(session.OpenedAt);
            session.ClosedAt = DateTime.UtcNow;
            session.ClosedBy = username;
            session.ClosingBalance = session.OpeningBalance + sales;
            session.ActualCash = actualCash;
            session.Notes = notes;
            session.Status = CashSessionStatuses.Closed;
            await _repo.UpdateAsync(session);
        }

        public async Task<List<CashSession>> GetSessionHistoryAsync(int count = 30)
        {
            if (_mediator != null)
                return await _mediator.Send(new GetSessionHistoryQuery(count));

            return await _repo!.GetHistoryAsync(count);
        }

        public async Task<decimal> GetTotalSalesForCurrentSessionAsync()
        {
            var session = await GetCurrentSessionAsync();
            if (session == null) return 0;
            return await _repo!.GetSalesTotalAsync(session.OpenedAt);
        }
    }
}
