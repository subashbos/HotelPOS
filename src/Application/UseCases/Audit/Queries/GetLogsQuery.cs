using AutoMapper;
using HotelPOS.Application.DTOs.Audit;
using HotelPOS.Application.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotelPOS.Application.UseCases.Audit.Queries
{
    public record GetLogsQuery(DateTime? From = null, DateTime? To = null) : IRequest<List<AuditLogDto>>;

    public class GetLogsQueryHandler : IRequestHandler<GetLogsQuery, List<AuditLogDto>>
    {
        private readonly IAuditRepository _repo;
        private readonly IMapper _mapper;

        public GetLogsQueryHandler(IAuditRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<AuditLogDto>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
        {
            var logs = await _repo.GetLogsAsync(request.From, request.To);
            return _mapper.Map<List<AuditLogDto>>(logs);
        }
    }
}
