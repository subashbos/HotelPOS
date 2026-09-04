using MapsterMapper;
using HotelPOS.Application.DTOs.Estimation;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Customer quotations/cost estimates, and converting them into real orders — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class EstimationsController : BaseApiController
    {
        private readonly IEstimationService _estimationService;
        private readonly IMapper _mapper;

        public EstimationsController(IEstimationService estimationService, IMapper mapper)
        {
            _estimationService = estimationService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EstimationDto>>> GetEstimations()
        {
            var estimations = await _estimationService.GetEstimationsAsync();
            return Ok(_mapper.Map<IEnumerable<EstimationDto>>(estimations));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<EstimationDto>> GetEstimation(int id)
        {
            if (id <= 0) return BadRequest("Invalid estimation ID.");

            var estimation = await _estimationService.GetEstimationByIdAsync(id);
            if (estimation == null) return NotFound();

            return Ok(_mapper.Map<EstimationDto>(estimation));
        }

        [HttpPost]
        public async Task<ActionResult<EstimationDto>> CreateEstimation([FromBody] SaveEstimationDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("Estimation must contain at least one item.");

            var estimation = BuildEstimation(0, request);

            try
            {
                await _estimationService.SaveEstimationAsync(estimation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetEstimation), new { id = estimation.Id }, _mapper.Map<EstimationDto>(estimation));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEstimation(int id, [FromBody] SaveEstimationDto request)
        {
            if (id <= 0) return BadRequest("Invalid estimation ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("Estimation must contain at least one item.");

            var estimation = BuildEstimation(id, request);

            try
            {
                await _estimationService.UpdateEstimationAsync(estimation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
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
        public async Task<IActionResult> DeleteEstimation(int id)
        {
            if (id <= 0) return BadRequest("Invalid estimation ID.");

            await _estimationService.DeleteEstimationAsync(id);
            return NoContent();
        }

        /// <summary>Converts an Accepted estimation into a real order and returns the new order's ID.</summary>
        [HttpPost("{id:int}/convert-to-order")]
        public async Task<ActionResult<object>> ConvertToOrder(int id)
        {
            if (id <= 0) return BadRequest("Invalid estimation ID.");

            try
            {
                var orderId = await _estimationService.ConvertToOrderAsync(id);
                return Ok(new { orderId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        // Totals are derived server-side from the line items rather than trusted from the client.
        private static Estimation BuildEstimation(int id, SaveEstimationDto request)
        {
            var estimation = new Estimation
            {
                Id = id,
                EstimationNumber = request.EstimationNumber,
                EstimationDate = request.EstimationDate,
                ValidUntil = request.ValidUntil,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                Status = string.IsNullOrWhiteSpace(request.Status) ? EstimationStatuses.Draft : request.Status,
                Notes = request.Notes,
                EstimationItems = request.Items.Select(i => new EstimationItem
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TaxPercentage = i.TaxPercentage,
                    Discount = i.Discount,
                    Total = Math.Round((i.Quantity * i.UnitPrice) * (1 + i.TaxPercentage / 100) - i.Discount, 2)
                }).ToList()
            };

            estimation.Subtotal = estimation.EstimationItems.Sum(i => i.Quantity * i.UnitPrice);
            estimation.TotalTax = estimation.EstimationItems.Sum(i => Math.Round(i.Quantity * i.UnitPrice * i.TaxPercentage / 100, 2));
            estimation.TotalDiscount = estimation.EstimationItems.Sum(i => i.Discount) + request.TotalDiscount;
            estimation.GrandTotal = Math.Max(estimation.Subtotal + estimation.TotalTax - estimation.TotalDiscount, 0);

            return estimation;
        }
    }
}
