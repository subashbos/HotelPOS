using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ILeaveRepository
    {
        Task<List<LeaveType>> GetLeaveTypesAsync();
        Task<LeaveType?> GetLeaveTypeByIdAsync(int id);

        Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, int year);
        Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, int year);
        Task AddBalanceAsync(LeaveBalance balance);
        Task UpdateBalanceAsync(LeaveBalance balance);

        Task<List<LeaveRequest>> GetRequestsAsync(int? employeeId = null, string? status = null);
        Task<LeaveRequest?> GetRequestByIdAsync(int id);
        Task AddRequestAsync(LeaveRequest request);
        Task UpdateRequestAsync(LeaveRequest request);
    }
}
