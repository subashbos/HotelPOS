using HotelPOS.Domain.Entities;
using HotelPOS.Application.DTOs.Audit;

namespace HotelPOS.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(string entityName, int entityId, string action, string? details = null);
        Task<List<AuditLogDto>> GetLogsAsync(DateTime? from = null, DateTime? to = null);
    }
}
