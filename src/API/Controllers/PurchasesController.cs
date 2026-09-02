using AutoMapper;
using HotelPOS.Application.DTOs.Purchase;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelPOS.Api.Controllers
{
    /// <summary>Purchase entries against suppliers — requires a valid JWT token on all endpoints.</summary>
    [Authorize]
    public class PurchasesController : BaseApiController
    {
        private readonly IPurchaseService _purchaseService;
        private readonly IMapper _mapper;

        public PurchasesController(IPurchaseService purchaseService, IMapper mapper)
        {
            _purchaseService = purchaseService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedPurchasesResponse>> GetPurchases([FromQuery] PurchaseListQueryRequest request)
        {
            var (purchases, totalCount) = await _purchaseService.GetPagedPurchasesAsync(
                request.Page ?? 1,
                request.PageSize ?? 20,
                new PurchaseQueryFilter(request.From, request.To, request.SupplierId, request.ItemName, request.PaymentType, request.InvoiceNo));

            return Ok(new PagedPurchasesResponse
            {
                Items = _mapper.Map<List<PurchaseDto>>(purchases),
                TotalCount = totalCount
            });
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
        {
            var suppliers = await _purchaseService.GetSuppliersAsync();
            return Ok(_mapper.Map<IEnumerable<SupplierDto>>(suppliers));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PurchaseDto>> GetPurchase(int id)
        {
            if (id <= 0) return BadRequest("Invalid purchase ID.");

            var purchase = await _purchaseService.GetPurchaseByIdAsync(id);
            if (purchase == null) return NotFound();

            return Ok(_mapper.Map<PurchaseDto>(purchase));
        }

        [HttpPost]
        public async Task<ActionResult<PurchaseDto>> CreatePurchase([FromBody] SavePurchaseDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("Purchase must contain at least one item.");

            var purchase = BuildPurchase(0, request);

            try
            {
                await _purchaseService.SavePurchaseAsync(purchase);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetPurchases), _mapper.Map<PurchaseDto>(purchase));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePurchase(int id, [FromBody] SavePurchaseDto request)
        {
            if (id <= 0) return BadRequest("Invalid purchase ID.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("Purchase must contain at least one item.");

            var purchase = BuildPurchase(id, request);

            try
            {
                await _purchaseService.UpdatePurchaseAsync(purchase);
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
        public async Task<IActionResult> DeletePurchase(int id)
        {
            if (id <= 0) return BadRequest("Invalid purchase ID.");

            await _purchaseService.DeletePurchaseAsync(id);
            return NoContent();
        }

        // Totals are derived server-side from the line items rather than trusted from the client.
        private static Purchase BuildPurchase(int id, SavePurchaseDto request)
        {
            var purchase = new Purchase
            {
                Id = id,
                SupplierId = request.SupplierId,
                InvoiceNumber = request.InvoiceNumber,
                PurchaseDate = request.PurchaseDate,
                PaymentType = request.PaymentType,
                Notes = request.Notes,
                PurchaseItems = request.Items.Select(i => new PurchaseItem
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

            purchase.Subtotal = purchase.PurchaseItems.Sum(i => i.Quantity * i.UnitPrice);
            purchase.TotalTax = purchase.PurchaseItems.Sum(i => Math.Round(i.Quantity * i.UnitPrice * i.TaxPercentage / 100, 2));
            purchase.TotalDiscount = purchase.PurchaseItems.Sum(i => i.Discount) + request.TotalDiscount;
            purchase.GrandTotal = Math.Max(purchase.Subtotal + purchase.TotalTax - purchase.TotalDiscount, 0);

            return purchase;
        }
    }

    public sealed class PurchaseListQueryRequest
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 20;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? SupplierId { get; set; }
        public string? ItemName { get; set; }
        public string? PaymentType { get; set; }
        public string? InvoiceNo { get; set; }
    }

    public sealed class PagedPurchasesResponse
    {
        public List<PurchaseDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
