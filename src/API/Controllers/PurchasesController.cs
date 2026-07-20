using AutoMapper;
using HotelPOS.Application.DTOs.Purchase;
using HotelPOS.Application.DTOs.Supplier;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
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
        public async Task<ActionResult<IEnumerable<PurchaseDto>>> GetPurchases()
        {
            var purchases = await _purchaseService.GetPurchasesAsync();
            return Ok(_mapper.Map<IEnumerable<PurchaseDto>>(purchases));
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers()
        {
            var suppliers = await _purchaseService.GetSuppliersAsync();
            return Ok(_mapper.Map<IEnumerable<SupplierDto>>(suppliers));
        }

        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<ActionResult<PurchaseDto>> CreatePurchase([FromBody] SavePurchaseDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (request.Items == null || request.Items.Count == 0)
                return BadRequest("Purchase must contain at least one item.");

            var purchase = new Purchase
            {
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

            // Totals are derived server-side from the line items rather than trusted from the client.
            purchase.Subtotal = purchase.PurchaseItems.Sum(i => i.Quantity * i.UnitPrice);
            purchase.TotalTax = purchase.PurchaseItems.Sum(i => Math.Round(i.Quantity * i.UnitPrice * i.TaxPercentage / 100, 2));
            purchase.TotalDiscount = purchase.PurchaseItems.Sum(i => i.Discount) + request.TotalDiscount;
            purchase.GrandTotal = Math.Max(purchase.Subtotal + purchase.TotalTax - purchase.TotalDiscount, 0);

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
    }
}
