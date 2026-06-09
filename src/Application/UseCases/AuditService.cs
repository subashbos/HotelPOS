using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _repo;
        private readonly IUserContext _userContext;

        public AuditService(IAuditRepository repo, IUserContext userContext)
        {
            _repo = repo;
            _userContext = userContext;
        }

        public async Task LogActionAsync(string entityName, int entityId, string action, string? details = null)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                throw new ArgumentException("Entity name cannot be empty or whitespace.", nameof(entityName));

            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action cannot be empty or whitespace.", nameof(action));

            var log = new AuditLog
            {
                EntityName = entityName.Trim(),
                EntityId = entityId,
                Action = action.Trim(),
                Timestamp = DateTime.UtcNow,
                Details = details,
                Username = string.IsNullOrWhiteSpace(_userContext.CurrentUsername) ? "System" : _userContext.CurrentUsername.Trim()
            };

            await _repo.AddAsync(log);
        }

        public async Task<List<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await _repo.GetLogsAsync(from, to);
        }
    }
}
