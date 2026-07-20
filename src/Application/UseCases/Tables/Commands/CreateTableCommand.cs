using MediatR;
using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using AutoMapper;

namespace HotelPOS.Application.UseCases.Tables.Commands
{
    public record CreateTableCommand(CreateTableDto Dto) : IRequest<int>;

    public class CreateTableCommandHandler : IRequestHandler<CreateTableCommand, int>
    {
        private readonly ITableRepository _repo;
        private readonly IMapper _mapper;

        public CreateTableCommandHandler(ITableRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<int> Handle(CreateTableCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == request.Dto.Number && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {request.Dto.Number} is already in use.");

            var table = _mapper.Map<Table>(request.Dto);
            return await _repo.AddAsync(table);
        }
    }
}
