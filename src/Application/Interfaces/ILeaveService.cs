using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ILeaveService
    {
        Task<List<LeaveType>> GetLeaveTypesAsync();
        Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, int year);
        Task<List<LeaveRequest>> GetRequestsAsync(int? employeeId = null, string? status = null);
        Task ApplyLeaveAsync(LeaveRequest request);
        Task ApproveLeaveAsync(int requestId, int approverEmployeeId);
        Task RejectLeaveAsync(int requestId, int approverEmployeeId, string reason);

        /// <summary>Withdraws the caller's own still-pending request, releasing the reserved balance hold.</summary>
        Task CancelLeaveAsync(int requestId, int employeeId);
    }
}
