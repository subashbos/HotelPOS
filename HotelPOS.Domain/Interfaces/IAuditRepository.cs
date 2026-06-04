namespace HotelPOS.Domain.Interfaces
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task<List<AuditLog>> GetLogsAsync(DateTime? from, DateTime? to);
    }
}
