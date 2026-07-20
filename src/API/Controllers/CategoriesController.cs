using AutoMapper;
using HotelPOS.Application.DTOs.Category;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Menu categories — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class CategoriesController : BaseApiController
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _categoryService.GetCategoriesAsync();
            return Ok(_mapper.Map<IEnumerable<CategoryDto>>(categories));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] SaveCategoryDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var id = await _categoryService.AddCategoryAsync(request.Name, request.DisplayOrder);
                var category = new CategoryDto { Id = id, Name = request.Name.Trim(), DisplayOrder = request.DisplayOrder };
                return CreatedAtAction(nameof(GetCategories), category);
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
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] SaveCategoryDto request)
        {
            if (id <= 0) return BadRequest("Invalid category ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                await _categoryService.UpdateCategoryAsync(id, request.Name, request.DisplayOrder);
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
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (id <= 0) return BadRequest("Invalid category ID.");

            try
            {
                await _categoryService.DeleteCategoryAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            return NoContent();
        }
    }
}
