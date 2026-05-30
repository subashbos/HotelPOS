using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.Persistence.Interfaces;
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
        private readonly IRepository<Item> _itemRepo;

        public ItemsController(IRepository<Item> itemRepo)
        {
            _itemRepo = itemRepo;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            var items = await _itemRepo.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            if (id <= 0) return BadRequest("Invalid item ID.");
            var item = await _itemRepo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST body uses CreateItemRequest DTO — never the raw domain entity
        [HttpPost]
        public async Task<ActionResult<Item>> CreateItem([FromBody] CreateItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var item = new Item
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                TaxPercentage = request.TaxPercentage,
                CategoryId = request.CategoryId,
                HsnCode = request.HsnCode,
                Barcode = request.Barcode,
                StockQuantity = request.StockQuantity,
                TrackInventory = request.TrackInventory
            };

            await _itemRepo.AddAsync(item);
            await _itemRepo.SaveChangesAsync();
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
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
