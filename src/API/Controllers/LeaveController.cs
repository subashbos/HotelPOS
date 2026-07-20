using AutoMapper;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Leave types, balances and requests — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class LeaveController : BaseApiController
    {
        private readonly ILeaveService _leaveService;
        private readonly IMapper _mapper;

        public LeaveController(ILeaveService leaveService, IMapper mapper)
        {
            _leaveService = leaveService;
            _mapper = mapper;
        }

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<LeaveTypeDto>>> GetLeaveTypes()
        {
            var types = await _leaveService.GetLeaveTypesAsync();
            return Ok(_mapper.Map<IEnumerable<LeaveTypeDto>>(types));
        }

        [HttpGet("balances/{employeeId:int}")]
        public async Task<ActionResult<IEnumerable<LeaveBalanceDto>>> GetBalances(int employeeId, [FromQuery] int? year)
        {
            if (employeeId <= 0) return BadRequest("Invalid employee ID.");
            var balances = await _leaveService.GetBalancesAsync(employeeId, year ?? DateTime.UtcNow.Year);
            return Ok(_mapper.Map<IEnumerable<LeaveBalanceDto>>(balances));
        }

        [HttpGet("requests")]
        public async Task<ActionResult<IEnumerable<LeaveRequestDto>>> GetRequests([FromQuery] int? employeeId, [FromQuery] string? status)
        {
            var requests = await _leaveService.GetRequestsAsync(employeeId, status);
            return Ok(_mapper.Map<IEnumerable<LeaveRequestDto>>(requests));
        }

        [HttpPost("requests")]
        public async Task<ActionResult<LeaveRequestDto>> ApplyLeave([FromBody] ApplyLeaveDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var leaveRequest = _mapper.Map<LeaveRequest>(request);
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

        [HttpPost("requests/{id:int}/approve")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> ApproveLeave(int id, [FromQuery] int approverEmployeeId)
        {
            if (id <= 0) return BadRequest("Invalid leave request ID.");
            if (approverEmployeeId <= 0) return BadRequest("A valid approver employee ID is required.");

            try
            {
                await _leaveService.ApproveLeaveAsync(id, approverEmployeeId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpPost("requests/{id:int}/reject")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> RejectLeave(int id, [FromQuery] int approverEmployeeId, [FromBody] RejectLeaveDto request)
        {
            if (id <= 0) return BadRequest("Invalid leave request ID.");
            if (approverEmployeeId <= 0) return BadRequest("A valid approver employee ID is required.");

            try
            {
                await _leaveService.RejectLeaveAsync(id, approverEmployeeId, request.Reason);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }
    }
}
