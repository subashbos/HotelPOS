using MapsterMapper;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>
    /// Employee self-service — leave, payslips and a limited profile edit, scoped to the caller's
    /// own linked Employee record. The employee ID is always resolved server-side from the JWT's
    /// CurrentUserId, never accepted from the client, so a caller can never reach another
    /// employee's data through this controller.
    /// </summary>
    [Authorize]
    public class EssController : BaseApiController
    {
        private const string NoLinkedEmployeeMessage = "No employee record is linked to this login.";

        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILeaveService _leaveService;
        private readonly IPayrollService _payrollService;
        private readonly IUserContext _userContext;
        private readonly IMapper _mapper;

        public EssController(
            IEmployeeRepository employeeRepository,
            ILeaveService leaveService,
            IPayrollService payrollService,
            IUserContext userContext,
            IMapper mapper)
        {
            _employeeRepository = employeeRepository;
            _leaveService = leaveService;
            _payrollService = payrollService;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<EmployeeDto>> GetProfile()
        {
            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);
            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] EssProfileUpdateDto request)
        {
            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            employee.Phone = request.Phone;
            employee.Address = request.Address;
            employee.EmergencyContactName = request.EmergencyContactName;
            employee.EmergencyContactPhone = request.EmergencyContactPhone;
            await _employeeRepository.UpdateAsync(employee);

            return NoContent();
        }

        [HttpGet("leave/types")]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes()
        {
            var types = await _leaveService.GetLeaveTypesAsync();
            return Ok(_mapper.Map<IEnumerable<LeaveTypeDto>>(types));
        }

        [HttpGet("leave/balances")]
        public async Task<ActionResult<IEnumerable<LeaveBalanceDto>>> GetLeaveBalances([FromQuery] int? year)
        {
            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            var balances = await _leaveService.GetBalancesAsync(employee.Id, year ?? DateTime.UtcNow.Year);
            return Ok(_mapper.Map<IEnumerable<LeaveBalanceDto>>(balances));
        }

        [HttpGet("leave/requests")]
        public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetLeaveRequests([FromQuery] string? status)
        {
            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            var requests = await _leaveService.GetRequestsAsync(employee.Id, status);
            return Ok(_mapper.Map<IEnumerable<LeaveRequestDto>>(requests));
        }

        [HttpPost("leave/requests")]
        public async Task<ActionResult<LeaveRequestDto>> ApplyLeave([FromBody] ApplyLeaveDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            var leaveRequest = _mapper.Map<LeaveRequest>(request);
            leaveRequest.EmployeeId = employee.Id; // never trust a client-supplied employeeId here
            try
            {
                await _leaveService.ApplyLeaveAsync(leaveRequest);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(ex.Message);
            }

            return Ok(_mapper.Map<LeaveRequestDto>(leaveRequest));
        }

        [HttpPost("leave/requests/{id:int}/cancel")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            if (id <= 0) return BadRequest("Invalid leave request ID.");

            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            try
            {
                await _leaveService.CancelLeaveAsync(id, employee.Id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpGet("payslips")]
        public async Task<ActionResult<IEnumerable<PayslipDto>>> GetPayslips()
        {
            var employee = await ResolveOwnEmployeeAsync();
            if (employee == null) return NotFound(NoLinkedEmployeeMessage);

            var payslips = await _payrollService.GetPayslipsByEmployeeAsync(employee.Id);
            return Ok(_mapper.Map<IEnumerable<PayslipDto>>(payslips));
        }

        private async Task<Employee?> ResolveOwnEmployeeAsync()
        {
            var userId = _userContext.CurrentUserId;
            if (userId == null) return null;
            return await _employeeRepository.GetByUserIdAsync(userId.Value);
        }
    }
}
