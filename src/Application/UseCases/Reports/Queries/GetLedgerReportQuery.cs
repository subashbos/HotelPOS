using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetLedgerReportQuery(DateTime From, DateTime To) : IRequest<List<LedgerReportRowDto>>;

    public class GetLedgerReportQueryHandler : IRequestHandler<GetLedgerReportQuery, List<LedgerReportRowDto>>
    {
        private readonly IReportService _reportService;

        public GetLedgerReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<List<LedgerReportRowDto>> Handle(GetLedgerReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetLedgerReportInternalAsync(request.From, request.To);
        }
    }
}
