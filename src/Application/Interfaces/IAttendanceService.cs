using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IAttendanceService
    {
        Task<List<Attendance>> GetAttendanceAsync(int employeeId, DateTime fromDate, DateTime toDate);
        Task<List<Attendance>> GetAttendanceForDateAsync(DateTime date);
        Task MarkAttendanceAsync(Attendance attendance);
        Task DeleteAttendanceAsync(int id);
    }
}
