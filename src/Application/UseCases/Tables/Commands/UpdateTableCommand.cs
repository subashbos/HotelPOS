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
    public record UpdateTableCommand(int Id, CreateTableDto Dto) : IRequest;

    public class UpdateTableCommandHandler : IRequestHandler<UpdateTableCommand>
    {
        private readonly ITableRepository _repo;
        private readonly IMapper _mapper;

        public UpdateTableCommandHandler(ITableRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task Handle(UpdateTableCommand request, CancellationToken cancellationToken)
        {
            var existing = await _repo.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == request.Dto.Number && t.Id != request.Id && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {request.Dto.Number} is already in use.");

            var table = await _repo.GetByIdAsync(request.Id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{request.Id} not found.");

            _mapper.Map(request.Dto, table);
            await _repo.UpdateAsync(table);
        }
    }
}
