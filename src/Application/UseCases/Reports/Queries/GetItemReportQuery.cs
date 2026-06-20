using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetItemReportQuery(DateTime? From = null, DateTime? To = null) : IRequest<List<ItemReportRowDto>>;

    public class GetItemReportQueryHandler : IRequestHandler<GetItemReportQuery, List<ItemReportRowDto>>
    {
        private readonly IReportService _reportService;

        public GetItemReportQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<List<ItemReportRowDto>> Handle(GetItemReportQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetItemReportInternalAsync(request.From, request.To);
        }
    }
}
