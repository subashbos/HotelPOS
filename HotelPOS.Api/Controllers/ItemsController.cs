using HotelPOS.Domain;
using HotelPOS.Persistence.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
            var item = await _itemRepo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Item>> CreateItem(Item item)
        {
            await _itemRepo.AddAsync(item);
            await _itemRepo.SaveChangesAsync();
            return CreatedAtAction(nameof(GetItem), new { id = item.Id }, item);
        }
    }
}
