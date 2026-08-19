using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelPOS.Infrastructure.Persistence
{
    public class LeaveRepository : ILeaveRepository
    {
        private readonly HotelDbContext _context;

        public LeaveRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveType>> GetLeaveTypesAsync()
        {
            return await _context.LeaveTypes.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        }

        public async Task<LeaveType?> GetLeaveTypeByIdAsync(int id)
        {
            return await _context.LeaveTypes.FindAsync(id);
        }

        public async Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, int year)
        {
            return await _context.LeaveBalances
                .AsNoTracking()
                .Include(b => b.LeaveType)
                .Where(b => b.EmployeeId == employeeId && b.Year == year)
                .ToListAsync();
        }

        // Left tracked: LeaveService mutates the returned balance in place and persists via
        // UpdateBalanceAsync's blind _context.LeaveBalances.Update(), which throws if this fetch
        // is untracked but the same row is already tracked elsewhere in the DbContext.
        public async Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, int year)
        {
            return await _context.LeaveBalances
                .Include(b => b.LeaveType)
                .FirstOrDefaultAsync(b => b.EmployeeId == employeeId && b.LeaveTypeId == leaveTypeId && b.Year == year);
        }

        public async Task AddBalanceAsync(LeaveBalance balance)
        {
            _context.LeaveBalances.Add(balance);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBalanceAsync(LeaveBalance balance)
        {
            _context.LeaveBalances.Update(balance);
            await _context.SaveChangesAsync();
        }

        public async Task<List<LeaveRequest>> GetRequestsAsync(int? employeeId = null, string? status = null)
        {
            var query = _context.LeaveRequests
                .AsNoTracking()
                .Include(r => r.Employee)
                .Include(r => r.LeaveType)
                .AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(r => r.EmployeeId == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            return await query.OrderByDescending(r => r.AppliedOn).ToListAsync();
        }

        // Left tracked: LeaveService.ApproveLeaveAsync/RejectLeaveAsync/CancelLeaveAsync mutate the
        // returned request in place and persist via UpdateRequestAsync's blind
        // _context.LeaveRequests.Update(), which throws if this fetch is untracked but the same
        // row is already tracked elsewhere in the DbContext.
        public async Task<LeaveRequest?> GetRequestByIdAsync(int id)
        {
            return await _context.LeaveRequests
                .Include(r => r.Employee)
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddRequestAsync(LeaveRequest request)
        {
            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRequestAsync(LeaveRequest request)
        {
            _context.LeaveRequests.Update(request);
            await _context.SaveChangesAsync();
        }
    }
}
