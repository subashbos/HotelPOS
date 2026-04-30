using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface IAuditService
    {
        Task LogActionAsync(string entityName, int entityId, string action, string? details = null);
        Task<List<AuditLog>> GetLogsAsync(DateTime? from = null, DateTime? to = null);
    }
}
