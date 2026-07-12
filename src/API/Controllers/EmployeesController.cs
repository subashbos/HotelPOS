using AutoMapper;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Employee master data — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class EmployeesController : BaseApiController
    {
        private readonly IEmployeeService _employeeService;
        private readonly IMapper _mapper;

        public EmployeesController(IEmployeeService employeeService, IMapper mapper)
        {
            _employeeService = employeeService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeDto>>(employees));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
        {
            if (id <= 0) return BadRequest("Invalid employee ID.");
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();
            return Ok(_mapper.Map<EmployeeDto>(employee));
        }

        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<DepartmentDto>>> GetDepartments()
        {
            var departments = await _employeeService.GetDepartmentsAsync();
            return Ok(_mapper.Map<IEnumerable<DepartmentDto>>(departments));
        }

        [HttpGet("designations")]
        public async Task<ActionResult<IEnumerable<DesignationDto>>> GetDesignations()
        {
            var designations = await _employeeService.GetDesignationsAsync();
            return Ok(_mapper.Map<IEnumerable<DesignationDto>>(designations));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] SaveEmployeeDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = _mapper.Map<Employee>(request);
            employee.Id = 0;
            try
            {
                await _employeeService.SaveEmployeeAsync(employee);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, _mapper.Map<EmployeeDto>(employee));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] SaveEmployeeDto request)
        {
            if (id <= 0) return BadRequest("Invalid employee ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var employee = _mapper.Map<Employee>(request);
            employee.Id = id;
            try
            {
                await _employeeService.SaveEmployeeAsync(employee);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            if (id <= 0) return BadRequest("Invalid employee ID.");
            try
            {
                await _employeeService.DeleteEmployeeAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
