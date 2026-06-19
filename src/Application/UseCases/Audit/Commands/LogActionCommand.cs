using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Audit.Commands
{
    public record LogActionCommand(string EntityName, int EntityId, string Action, string? Details = null) : IRequest;

    public class LogActionCommandHandler : IRequestHandler<LogActionCommand>
    {
        private readonly IAuditRepository _repo;
        private readonly IUserContext _userContext;

        public LogActionCommandHandler(IAuditRepository repo, IUserContext userContext)
        {
            _repo = repo;
            _userContext = userContext;
        }

        public async Task Handle(LogActionCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.EntityName))
                throw new ArgumentException("Entity name cannot be empty or whitespace.", nameof(request.EntityName));

            if (string.IsNullOrWhiteSpace(request.Action))
                throw new ArgumentException("Action cannot be empty or whitespace.", nameof(request.Action));

            var log = new AuditLog
            {
                EntityName = request.EntityName.Trim(),
                EntityId = request.EntityId,
                Action = request.Action.Trim(),
                Timestamp = DateTime.UtcNow,
                Details = request.Details,
                Username = string.IsNullOrWhiteSpace(_userContext.CurrentUsername) ? "System" : _userContext.CurrentUsername.Trim()
            };

            await _repo.AddAsync(log);
        }
    }
}
