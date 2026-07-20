using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetGstReportQuery(DateTime From, DateTime To) : IRequest<List<GstReportRowDto>>;

    public class GetGstReportQueryHandler : IRequestHandler<GetGstReportQuery, List<GstReportRowDto>>
    {
        private readonly IReportService _reportService;

        public GetGstReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<List<GstReportRowDto>> Handle(GetGstReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetGstReportInternalAsync(request.From, request.To);
        }
    }
}
