using AutoMapper;
using HotelPOS.Application.DTOs.UnitOfMeasurement;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Units of measurement — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class UnitOfMeasurementsController : BaseApiController
    {
        private readonly IUnitOfMeasurementService _unitService;
        private readonly IMapper _mapper;

        public UnitOfMeasurementsController(IUnitOfMeasurementService unitService, IMapper mapper)
        {
            _unitService = unitService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UnitOfMeasurementDto>>> GetUnitOfMeasurements()
        {
            var units = await _unitService.GetUnitsAsync();
            return Ok(_mapper.Map<IEnumerable<UnitOfMeasurementDto>>(units));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<UnitOfMeasurementDto>> CreateUnitOfMeasurement([FromBody] SaveUnitOfMeasurementDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var id = await _unitService.AddUnitAsync(request.Name, request.DisplayOrder);
                var unit = new UnitOfMeasurementDto { Id = id, Name = request.Name.Trim(), DisplayOrder = request.DisplayOrder };
                return CreatedAtAction(nameof(GetUnitOfMeasurements), unit);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> UpdateUnitOfMeasurement(int id, [FromBody] SaveUnitOfMeasurementDto request)
        {
            if (id <= 0) return BadRequest("Invalid unit ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _unitService.UpdateUnitAsync(id, request.Name, request.DisplayOrder);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
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
        public async Task<IActionResult> DeleteUnitOfMeasurement(int id)
        {
            if (id <= 0) return BadRequest("Invalid unit ID.");

            try
            {
                await _unitService.DeleteUnitAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return NoContent();
        }
    }
}
