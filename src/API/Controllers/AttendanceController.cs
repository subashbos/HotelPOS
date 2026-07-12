using AutoMapper;
using HotelPOS.Application.DTOs.Attendance;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Employee attendance — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class AttendanceController : BaseApiController
    {
        private readonly IAttendanceService _attendanceService;
        private readonly IMapper _mapper;

        public AttendanceController(IAttendanceService attendanceService, IMapper mapper)
        {
            _attendanceService = attendanceService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendance(
            [FromQuery] int employeeId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (employeeId <= 0) return BadRequest("Invalid employee ID.");

            try
            {
                var records = await _attendanceService.GetAttendanceAsync(employeeId, from, to);
                return Ok(_mapper.Map<IEnumerable<AttendanceDto>>(records));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var attendance = _mapper.Map<Attendance>(request);
            try
            {
                await _attendanceService.MarkAttendanceAsync(attendance);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(_mapper.Map<AttendanceDto>(attendance));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> DeleteAttendance(int id)
        {
            if (id <= 0) return BadRequest("Invalid attendance ID.");
            try
            {
                await _attendanceService.DeleteAttendanceAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
