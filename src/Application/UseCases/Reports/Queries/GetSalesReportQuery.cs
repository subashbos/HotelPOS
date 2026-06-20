using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetSalesReportQuery(DateTime? From = null, DateTime? To = null) : IRequest<SalesReportDto>;

    public class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportDto>
    {
        private readonly IReportService _reportService;

        public GetSalesReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<SalesReportDto> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetSalesReportInternalAsync(request.From, request.To);
        }
    }
}
