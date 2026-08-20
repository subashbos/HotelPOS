using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetGstR1ReportQuery(DateTime From, DateTime To) : IRequest<GstR1ReportDto>;

    public class GetGstR1ReportQueryHandler : IRequestHandler<GetGstR1ReportQuery, GstR1ReportDto>
    {
        private readonly IReportService _reportService;

        public GetGstR1ReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<GstR1ReportDto> Handle(GetGstR1ReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetGstR1ReportInternalAsync(request.From, request.To);
        }
    }
}
