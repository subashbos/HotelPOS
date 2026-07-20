using AutoMapper;
using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Customer relationship management — profiles and order history. Requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class CustomersController : BaseApiController
    {
        private const string InvalidCustomerId = "Invalid customer ID.";

        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;

        public CustomersController(ICustomerService customerService, IMapper mapper)
        {
            _customerService = customerService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers([FromQuery] bool includeInactive = false)
        {
            var customers = await _customerService.GetCustomersAsync(includeInactive);
            return Ok(_mapper.Map<IEnumerable<CustomerDto>>(customers));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            if (id <= 0) return BadRequest(InvalidCustomerId);
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return Ok(_mapper.Map<CustomerDto>(customer));
        }

        [HttpGet("by-phone/{phone}")]
        public async Task<ActionResult<CustomerDto>> GetCustomerByPhone(string phone)
        {
            var customer = await _customerService.GetCustomerByPhoneAsync(phone);
            if (customer == null) return NotFound();
            return Ok(_mapper.Map<CustomerDto>(customer));
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<CustomerHistoryDto>> GetCustomerHistory(int id)
        {
            if (id <= 0) return BadRequest(InvalidCustomerId);
            try
            {
                return Ok(await _customerService.GetCustomerHistoryAsync(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Cashier}")]
        public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] SaveCustomerDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customer = _mapper.Map<Customer>(request);
            customer.Id = 0;
            try
            {
                await _customerService.SaveCustomerAsync(customer);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, _mapper.Map<CustomerDto>(customer));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Cashier}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] SaveCustomerDto request)
        {
            if (id <= 0) return BadRequest(InvalidCustomerId);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customer = _mapper.Map<Customer>(request);
            customer.Id = id;
            try
            {
                await _customerService.SaveCustomerAsync(customer);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0) return BadRequest(InvalidCustomerId);
            try
            {
                await _customerService.DeleteCustomerAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
