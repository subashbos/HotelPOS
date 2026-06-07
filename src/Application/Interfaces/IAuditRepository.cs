using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetLogsAsync(DateTime? from, DateTime? to);
    }
}
