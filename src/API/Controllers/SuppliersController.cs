using AutoMapper;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Supplier master data — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class SuppliersController : BaseApiController
    {
        private readonly ISupplierService _supplierService;
        private readonly IMapper _mapper;

        public SuppliersController(ISupplierService supplierService, IMapper mapper)
        {
            _supplierService = supplierService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
        {
            var suppliers = await _supplierService.GetSuppliersAsync();
            return Ok(_mapper.Map<IEnumerable<SupplierDto>>(suppliers));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
        {
            if (id <= 0) return BadRequest("Invalid supplier ID.");
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();
            return Ok(_mapper.Map<SupplierDto>(supplier));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] SaveSupplierDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var supplier = _mapper.Map<Supplier>(request);
            supplier.Id = 0;
            try
            {
                await _supplierService.SaveSupplierAsync(supplier);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, _mapper.Map<SupplierDto>(supplier));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] SaveSupplierDto request)
        {
            if (id <= 0) return BadRequest("Invalid supplier ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var supplier = _mapper.Map<Supplier>(request);
            supplier.Id = id;
            try
            {
                await _supplierService.SaveSupplierAsync(supplier);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (id <= 0) return BadRequest("Invalid supplier ID.");
            try
            {
                await _supplierService.DeleteSupplierAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return NoContent();
        }
    }
}
