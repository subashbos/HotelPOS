using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Reports.Queries
{
    public record GetMonthlySalesChartQuery() : IRequest<List<MonthlySalesChartDto>>;

    public class GetMonthlySalesChartQueryHandler : IRequestHandler<GetMonthlySalesChartQuery, List<MonthlySalesChartDto>>
    {
        private readonly IReportService _reportService;

        public GetMonthlySalesChartQueryHandler(IReportService reportService)
        {
            _reportService = reportService;
        }

        public async Task<List<MonthlySalesChartDto>> Handle(GetMonthlySalesChartQuery request, CancellationToken cancellationToken)
        {
            return await _reportService.GetMonthlyChartDataInternalAsync();
        }
    }
}
