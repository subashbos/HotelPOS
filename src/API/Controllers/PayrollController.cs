using AutoMapper;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Salary structures and monthly payroll runs — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class PayrollController : BaseApiController
    {
        private readonly IPayrollService _payrollService;
        private readonly IMapper _mapper;

        public PayrollController(IPayrollService payrollService, IMapper mapper)
        {
            _payrollService = payrollService;
            _mapper = mapper;
        }

        [HttpGet("salary-structures/{employeeId:int}")]
        public async Task<ActionResult<IEnumerable<SalaryStructureDto>>> GetSalaryStructures(int employeeId)
        {
            if (employeeId <= 0) return BadRequest("Invalid employee ID.");
            var structures = await _payrollService.GetSalaryStructuresAsync(employeeId);
            return Ok(_mapper.Map<IEnumerable<SalaryStructureDto>>(structures));
        }

        [HttpPost("salary-structures")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> SaveSalaryStructure([FromBody] SaveSalaryStructureDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var structure = _mapper.Map<SalaryStructure>(request);
            try
            {
                await _payrollService.SaveSalaryStructureAsync(structure);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(_mapper.Map<SalaryStructureDto>(structure));
        }

        [HttpPost("run")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<ActionResult<PayrollRunDto>> RunPayroll([FromBody] RunPayrollDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var run = await _payrollService.RunPayrollAsync(request.Month, request.Year, null);
                return Ok(_mapper.Map<PayrollRunDto>(run));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("runs/{id:int}/mark-paid")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> MarkRunAsPaid(int id)
        {
            if (id <= 0) return BadRequest("Invalid payroll run ID.");

            try
            {
                await _payrollService.MarkRunAsPaidAsync(id);
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

        [HttpGet("runs")]
        public async Task<ActionResult<IEnumerable<PayrollRunDto>>> GetRuns()
        {
            var runs = await _payrollService.GetRunsAsync();
            return Ok(_mapper.Map<IEnumerable<PayrollRunDto>>(runs));
        }

        [HttpGet("runs/{id:int}")]
        public async Task<ActionResult<PayrollRunDto>> GetRun(int id)
        {
            if (id <= 0) return BadRequest("Invalid payroll run ID.");
            var run = await _payrollService.GetRunByIdAsync(id);
            if (run == null) return NotFound();
            return Ok(_mapper.Map<PayrollRunDto>(run));
        }

        [HttpGet("payslips/{employeeId:int}")]
        public async Task<ActionResult<IEnumerable<PayslipDto>>> GetPayslips(int employeeId)
        {
            if (employeeId <= 0) return BadRequest("Invalid employee ID.");
            var payslips = await _payrollService.GetPayslipsByEmployeeAsync(employeeId);
            return Ok(_mapper.Map<IEnumerable<PayslipDto>>(payslips));
        }
    }
}
