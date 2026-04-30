using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;

namespace HotelPOS.Application
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
            var log = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                Details = details,
                Username = _userContext.CurrentUsername
            };

            await _repo.AddAsync(log);
        }

        public async Task<List<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            return await _repo.GetLogsAsync(from, to);
        }
    }
}
