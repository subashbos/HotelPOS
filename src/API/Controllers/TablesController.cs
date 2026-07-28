using AutoMapper;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Dining table master data — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class TablesController : BaseApiController
    {
        private readonly ITableService _tableService;
        private readonly IMapper _mapper;

        public TablesController(ITableService tableService, IMapper mapper)
        {
            _tableService = tableService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableDto>>> GetTables()
        {
            var tables = await _tableService.GetTablesAsync();
            return Ok(_mapper.Map<IEnumerable<TableDto>>(tables));
        }

        [HttpPost]
        public async Task<ActionResult<TableDto>> CreateTable([FromBody] CreateTableDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var id = await _tableService.AddTableAsync(request);
                var table = _mapper.Map<TableDto>(request);
                table.Id = id;
                return CreatedAtAction(nameof(GetTables), table);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateTable(int id, [FromBody] CreateTableDto request)
        {
            if (id <= 0) return BadRequest("Invalid table ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _tableService.UpdateTableAsync(id, request);
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            if (id <= 0) return BadRequest("Invalid table ID.");

            try
            {
                await _tableService.DeleteTableAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
