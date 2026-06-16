using HotelPOS.Domain.Entities;
using MediatR;
using HotelPOS.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Orders.Queries
{
    public record GetOrdersQuery(
        int PageNumber,
        int PageSize,
        DateTime? From = null,
        DateTime? To = null,
        int? TableNumber = null,
        string? Search = null,
        string? PaymentMode = null,
        string? OrderType = null,
        int? CategoryId = null
    ) : IRequest<(List<Order> Items, int TotalCount)>;

    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, (List<Order> Items, int TotalCount)>
    {
        private readonly IOrderService _orderService;

        public GetOrdersQueryHandler(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public async Task<(List<Order> Items, int TotalCount)> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            return await _orderService.GetPagedOrdersAsync(
                request.PageNumber,
                request.PageSize,
                request.From,
                request.To,
                request.TableNumber,
                request.Search,
                request.PaymentMode,
                request.OrderType,
                request.CategoryId
            );
        }
    }
}
