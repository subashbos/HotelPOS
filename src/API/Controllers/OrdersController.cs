using HotelPOS.Application.DTOs.Order;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Orders.Commands;
using HotelPOS.Application.UseCases.Orders.Queries;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HotelPOS.Api.Controllers
{
    [Authorize]
    public class OrdersController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly IUserContext _userContext;
        private readonly AutoMapper.IMapper _mapper;

        private const string InvalidOrderIdErrorMessage = "Invalid order ID.";

        public OrdersController(IMediator mediator, IUserContext userContext, AutoMapper.IMapper mapper)
        {
            _mediator = mediator;
            _userContext = userContext;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<PagedOrdersResponse>> GetPagedOrders([FromQuery] GetOrdersQueryRequest request)
        {
            var query = new GetOrdersQuery(
                request.PageNumber ?? 1,
                request.PageSize ?? 10,
                request.From,
                request.To,
                request.TableNumber,
                request.Search,
                request.PaymentMode,
                request.OrderType,
                request.CategoryId
            );

            var (items, totalCount) = await _mediator.Send(query);
            return Ok(new PagedOrdersResponse
            {
                Items = _mapper.Map<List<OrderDto>>(items),
                TotalCount = totalCount
            });
        }

        [HttpPost]
        public async Task<ActionResult<int>> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var command = _mapper.Map<CreateOrderCommand>(request);

            var orderId = await _mediator.Send(command);
            return Ok(orderId);
        }

        [HttpPost("{id:int}/void")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> VoidOrder(int id, [FromBody] VoidOrderRequest request)
        {
            if (id <= 0) return BadRequest(InvalidOrderIdErrorMessage);
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Reason for voiding the order is required.");

            var currentUser = _userContext.CurrentUsername ?? "API User";
            var command = new VoidOrderCommand(id, request.Reason, currentUser);
            await _mediator.Send(command);
            return NoContent();
        }

        // Money-back action - same role gate as Void.
        [HttpPost("{id:int}/refund")]
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
        public async Task<IActionResult> RefundOrder(int id, [FromBody] RefundOrderRequest request)
        {
            if (id <= 0) return BadRequest(InvalidOrderIdErrorMessage);
            if (request.Items == null || request.Items.Count == 0) return BadRequest("At least one item is required to refund.");
            if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest("Reason for the refund is required.");

            var itemsToRefund = request.Items.Select(i => new OrderItemRefundDto(i.ItemId, i.QuantityToRefund)).ToList();
            var command = new RefundOrderCommand(id, itemsToRefund, request.Reason);
            await _mediator.Send(command);
            return NoContent();
        }

        // Recording money already collected (e.g. completing a split/partial payment) - same
        // authorization level as creating an order, not restricted to Admin/Manager.
        [HttpPost("{id:int}/payment")]
        public async Task<IActionResult> ProcessPartialPayment(int id, [FromBody] ProcessPartialPaymentRequest request)
        {
            if (id <= 0) return BadRequest(InvalidOrderIdErrorMessage);
            if (request.Cash < 0 || request.Card < 0 || request.Upi < 0) return BadRequest("Payment amounts cannot be negative.");
            if (request.Cash == 0 && request.Card == 0 && request.Upi == 0) return BadRequest("At least one payment amount is required.");

            var command = new ProcessPartialPaymentCommand(id, request.Cash, request.Card, request.Upi);
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderRequest request)
        {
            if (id <= 0) return BadRequest(InvalidOrderIdErrorMessage);
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = new Order
            {
                Id = id,
                Items = _mapper.Map<List<OrderItem>>(request.Items),
                TableNumber = request.TableNumber,
                DiscountAmount = request.Discount,
                PaymentMode = request.PaymentMode,
                OrderType = request.OrderType,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerGstin = request.CustomerGstin
            };

            await _mediator.Send(new UpdateOrderCommand(order));
            return NoContent();
        }
    }

    public sealed class PagedOrdersResponse
    {
        public List<OrderDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public sealed class CreateOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public List<OrderItemDto> Items { get; set; } = new();

        // sonar: an omitted value defaults to 0, but CreateOrderCommandValidator already
        // rejects TableNumber == 0 for DineIn orders and caps Discount to [0, subtotal],
        // so under-posting either field can't produce an invalid or exploitable order.
        public int TableNumber { get; set; } // NOSONAR

        public decimal Discount { get; set; } // NOSONAR

        public string PaymentMode { get; set; } = PaymentModes.Cash;

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public string? CustomerGstin { get; set; }

        public string OrderType { get; set; } = OrderTypes.DineIn;
    }

    public sealed class VoidOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class RefundOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public List<RefundOrderItemRequest> Items { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Required]
        public string Reason { get; set; } = string.Empty;
    }

    public sealed class RefundOrderItemRequest
    {
        public int ItemId { get; set; } // NOSONAR
        public int QuantityToRefund { get; set; } // NOSONAR
    }

    public sealed class ProcessPartialPaymentRequest
    {
        public decimal Cash { get; set; } // NOSONAR
        public decimal Card { get; set; } // NOSONAR
        public decimal Upi { get; set; } // NOSONAR
    }

    public sealed class UpdateOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public List<OrderItemDto> Items { get; set; } = new();

        public int TableNumber { get; set; } // NOSONAR

        public decimal Discount { get; set; } // NOSONAR

        public string PaymentMode { get; set; } = PaymentModes.Cash;

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public string? CustomerGstin { get; set; }

        public string OrderType { get; set; } = OrderTypes.DineIn;
    }

    public sealed class GetOrdersQueryRequest
    {
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? TableNumber { get; set; }
        public string? Search { get; set; }
        public string? PaymentMode { get; set; }
        public string? OrderType { get; set; }
        public int? CategoryId { get; set; }
    }
}
