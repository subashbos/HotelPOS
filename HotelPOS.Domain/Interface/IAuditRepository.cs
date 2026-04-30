using HotelPOS.Domain;

namespace HotelPOS.Domain.Interface
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetLogsAsync(DateTime? from, DateTime? to);
    }
}
