using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _repository;
        private readonly IAuthorizationService _authorization;

        public AttendanceService(IAttendanceRepository repository, IAuthorizationService authorization)
        {
            _repository = repository;
            _authorization = authorization;
        }

        public async Task<List<Attendance>> GetAttendanceAsync(int employeeId, DateTime fromDate, DateTime toDate)
        {
            _authorization.EnsurePermission(PermissionModules.HrAttendance);
            if (employeeId <= 0) throw new ArgumentException("A valid employee is required.");
            if (toDate.Date < fromDate.Date) throw new ArgumentException("To Date cannot be before From Date.");

            return await _repository.GetByEmployeeAsync(employeeId, fromDate, toDate);
        }

        public async Task<List<Attendance>> GetAttendanceForDateAsync(DateTime date)
        {
            return await _repository.GetByDateRangeAsync(date.Date, date.Date);
        }

        public async Task<Attendance> MarkAttendanceAsync(Attendance attendance)
        {
            if (attendance == null) throw new ArgumentNullException(nameof(attendance));
            if (attendance.EmployeeId <= 0) throw new ArgumentException("A valid employee is required.");
            if (!AttendanceStatuses.All.Contains(attendance.Status))
                throw new ArgumentException($"Invalid attendance status '{attendance.Status}'.");

            attendance.Date = attendance.Date.Date;

            if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
            {
                var worked = attendance.CheckOutTime.Value - attendance.CheckInTime.Value;
                attendance.WorkedHours = worked.TotalHours > 0 ? Math.Round((decimal)worked.TotalHours, 2) : 0;
            }

            var existing = await _repository.GetByEmployeeAndDateAsync(attendance.EmployeeId, attendance.Date);
            if (existing != null)
            {
                existing.CheckInTime = attendance.CheckInTime;
                existing.CheckOutTime = attendance.CheckOutTime;
                existing.Status = attendance.Status;
                existing.WorkedHours = attendance.WorkedHours;
                existing.Remarks = attendance.Remarks?.Trim();
                await _repository.UpdateAsync(existing);
                return existing;
            }

            attendance.Remarks = attendance.Remarks?.Trim();
            await _repository.AddAsync(attendance);
            return attendance;
        }

        public async Task DeleteAttendanceAsync(int id)
        {
            _ = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Attendance record #{id} not found.");
            await _repository.DeleteAsync(id);
        }
    }
}
