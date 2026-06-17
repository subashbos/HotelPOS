using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.UseCases.Orders.Queries;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Api.Controllers
{
    [Authorize]
    public class OrdersController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly IUserContext _userContext;
        private readonly AutoMapper.IMapper _mapper;

        public OrdersController(IMediator mediator, IUserContext userContext, AutoMapper.IMapper mapper)
        {
            _mediator = mediator;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedOrdersResponse>> GetPagedOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int? tableNumber = null,
            [FromQuery] string? search = null,
            [FromQuery] string? paymentMode = null,
            [FromQuery] string? orderType = null,
            [FromQuery] int? categoryId = null)
        {
            var query = new GetOrdersQuery(
                pageNumber,
                pageSize,
                from,
                to,
                tableNumber,
                search,
                paymentMode,
                orderType,
                categoryId
            );

            var (items, totalCount) = await _mediator.Send(query);
            return Ok(new PagedOrdersResponse
            {
                Items = items,
                TotalCount = totalCount
            });
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var command = _mapper.Map<CreateOrderCommand>(request);

                var orderId = await _mediator.Send(command);
                return Ok(orderId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id:int}/void")]
        public async Task<IActionResult> VoidOrder(int id, [FromBody] VoidOrderRequest request)
        {
            if (id <= 0) return BadRequest("Invalid order ID.");
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Reason for voiding the order is required.");

            try
            {
                var currentUser = _userContext.CurrentUsername ?? "API User";
                var command = new VoidOrderCommand(id, request.Reason, currentUser);
                await _mediator.Send(command);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }

    public sealed class PagedOrdersResponse
    {
        public List<Order> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public sealed class CreateOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public List<OrderItem> Items { get; set; } = new();
        
        public int TableNumber { get; set; }
        
        public decimal Discount { get; set; }
        
        public string PaymentMode { get; set; } = "Cash";
        
        public string? CustomerName { get; set; }
        
        public string? CustomerPhone { get; set; }
        
        public string? CustomerGstin { get; set; }
        
        public string OrderType { get; set; } = "DineIn";
    }

    public sealed class VoidOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Reason { get; set; } = string.Empty;
    }
}
