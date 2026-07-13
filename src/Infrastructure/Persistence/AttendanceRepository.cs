using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly HotelDbContext _context;

        public AttendanceRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Attendance>> GetByEmployeeAsync(int employeeId, DateTime fromDate, DateTime toDate)
        {
            return await _context.Attendances
                .Where(a => a.EmployeeId == employeeId && a.Date >= fromDate.Date && a.Date <= toDate.Date)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public async Task<List<Attendance>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Date >= fromDate.Date && a.Date <= toDate.Date)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        public async Task<Attendance?> GetByEmployeeAndDateAsync(int employeeId, DateTime date)
        {
            return await _context.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date.Date);
        }

        public async Task<Attendance?> GetByIdAsync(int id)
        {
            return await _context.Attendances.Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAsync(Attendance attendance)
        {
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Attendance attendance)
        {
            _context.Attendances.Update(attendance);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance != null)
            {
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
            }
        }
    }
}
