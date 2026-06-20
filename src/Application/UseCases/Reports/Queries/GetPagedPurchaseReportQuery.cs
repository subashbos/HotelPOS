using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetPagedPurchaseReportQuery(
        int Page,
        int PageSize,
        DateTime? From,
        DateTime? To,
        int? SupplierId,
        string? ItemName,
        string? PaymentType,
        string? InvoiceNo
    ) : IRequest<(List<PurchaseReportRowDto> items, int totalCount, decimal totalPurchases, decimal totalTax, decimal totalDiscount, int totalQty)>;

    public class GetPagedPurchaseReportQueryHandler : IRequestHandler<
        GetPagedPurchaseReportQuery, 
        (List<PurchaseReportRowDto> items, int totalCount, decimal totalPurchases, decimal totalTax, decimal totalDiscount, int totalQty)
    >
    {
        private readonly IReportService _reportService;

        public GetPagedPurchaseReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<(List<PurchaseReportRowDto> items, int totalCount, decimal totalPurchases, decimal totalTax, decimal totalDiscount, int totalQty)> Handle(
            GetPagedPurchaseReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetPagedPurchaseReportInternalAsync(new PagedPurchaseReportRequest(
                request.Page,
                request.PageSize,
                request.From,
                request.To,
                request.SupplierId,
                request.ItemName,
                request.PaymentType,
                request.InvoiceNo
            ));
        }
    }
}
