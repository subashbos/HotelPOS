using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Items.Queries;
using HotelPOS.Domain.Common.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>
    /// Items catalogue API — requires a valid JWT token on all endpoints.
    /// Uses a DTO for input to ensure business validation in the service layer.
    /// </summary>
    [Authorize]
    public class ItemsController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly AutoMapper.IMapper _mapper;

        public ItemsController(IMediator mediator, AutoMapper.IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems()
        {
            var items = await _mediator.Send(new GetItemsQuery());
            return Ok(_mapper.Map<IEnumerable<ItemDto>>(items));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ItemDto>> GetItem(int id)
        {
            if (id <= 0) return BadRequest("Invalid item ID.");
            var item = await _mediator.Send(new GetItemByIdQuery(id));
            if (item == null) return NotFound();
            return Ok(_mapper.Map<ItemDto>(item));
        }

        // POST body uses CreateItemRequest DTO — never the raw domain entity
        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<ItemDto>> CreateItem([FromBody] CreateItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var command = _mapper.Map<CreateItemCommand>(request);

            var item = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, _mapper.Map<ItemDto>(item));
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<ItemDto>> UpdateItem(int id, [FromBody] CreateItemRequest request)
        {
            if (id <= 0) return BadRequest("Invalid item ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var command = new UpdateItemCommand(
                id,
                request.Name,
                request.Price,
                request.TaxPercentage,
                request.CategoryId,
                request.HsnCode,
                request.Barcode,
                request.StockQuantity,
                request.TrackInventory);

            try
            {
                var item = await _mediator.Send(command);
                return Ok(_mapper.Map<ItemDto>(item));
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
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> DeleteItem(int id)
        {
            if (id <= 0) return BadRequest("Invalid item ID.");

            try
            {
                await _mediator.Send(new DeleteItemCommand(id));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return NoContent();
        }
    }

    /// <summary>DTO to prevent binding raw domain entities from HTTP requests.</summary>
    public sealed class CreateItemRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 100, ErrorMessage = "Tax percentage must be between 0 and 100.")]
        public decimal TaxPercentage { get; set; }

        public int? CategoryId { get; set; }
        public string? HsnCode { get; set; }
        public string? Barcode { get; set; }
        public int StockQuantity { get; set; } = 0;
        public bool TrackInventory { get; set; } = false;
    }
}
