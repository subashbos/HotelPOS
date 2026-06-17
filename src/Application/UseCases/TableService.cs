using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using FluentValidation;
using AutoMapper;
using HotelPOS.Application.Common.Validators;
using MediatR;
using HotelPOS.Application.UseCases.Tables.Commands;
using HotelPOS.Application.UseCases.Tables.Queries;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

using Microsoft.Extensions.DependencyInjection;

namespace HotelPOS.Application.UseCases
{
    public class TableService : ITableService
    {
        private readonly IMediator? _mediator;
        private readonly ITableRepository? _tableRepository;
        private readonly IValidator<CreateTableDto>? _validator;
        private readonly IMapper? _mapper;

        public TableService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public TableService(ITableRepository tableRepository, IValidator<CreateTableDto>? validator = null, IMapper? mapper = null)
        {
            _tableRepository = tableRepository;
            _validator = validator ?? new CreateTableDtoValidator();
            
            if (mapper == null)
            {
                var cfg = new AutoMapper.MapperConfiguration(
                    expr => expr.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
                    Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
                _mapper = cfg.CreateMapper();
            }
            else
            {
                _mapper = mapper;
            }
        }

        public async Task<int> AddTableAsync(CreateTableDto dto)
        {
            if (_mediator != null)
                return await _mediator.Send(new CreateTableCommand(dto));

            var result = _validator!.Validate(dto);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors.First().ErrorMessage);

            var existing = await _tableRepository!.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == dto.Number && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {dto.Number} is already in use.");

            var table = _mapper!.Map<Table>(dto);
            return await _tableRepository.AddAsync(table);
        }

        public async Task<List<Table>> GetTablesAsync()
        {
            if (_mediator != null)
                return await _mediator.Send(new GetTablesQuery());

            return await _tableRepository!.GetAllAsync() ?? new List<Table>();
        }

        public async Task UpdateTableAsync(int id, CreateTableDto dto)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new UpdateTableCommand(id, dto));
                return;
            }

            var result = _validator!.Validate(dto);
            if (!result.IsValid)
                throw new ArgumentException(result.Errors.First().ErrorMessage);

            var existing = await _tableRepository!.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == dto.Number && t.Id != id && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {dto.Number} is already in use.");

            var table = await _tableRepository.GetByIdAsync(id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{id} not found.");

            _mapper!.Map(dto, table);
            await _tableRepository.UpdateAsync(table);
        }

        public async Task DeleteTableAsync(int id)
        {
            if (_mediator != null)
            {
                await _mediator.Send(new DeleteTableCommand(id));
                return;
            }

            var table = await _tableRepository!.GetByIdAsync(id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{id} not found or already deleted.");

            await _tableRepository.DeleteAsync(id);
        }
    }
}
