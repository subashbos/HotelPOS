using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IAttendanceRepository
    {
        Task<List<Attendance>> GetByEmployeeAsync(int employeeId, DateTime fromDate, DateTime toDate);
        Task<List<Attendance>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date);
        Task<Attendance?> GetByIdAsync(int id);
        Task AddAsync(Attendance attendance);
        Task UpdateAsync(Attendance attendance);
        Task DeleteAsync(int id);
    }
}
