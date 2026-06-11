using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class TableService : ITableService
    {
        private readonly ITableRepository _tableRepository;

        public TableService(ITableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task<int> AddTableAsync(CreateTableDto dto)
        {
            if (dto.Number <= 0)
                throw new ArgumentException("Table number must be greater than zero.");

            var existing = await _tableRepository.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == dto.Number && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {dto.Number} is already in use.");

            var table = new Table
            {
                Number = dto.Number,
                Name = dto.Name,
                Capacity = dto.Capacity,
                IsActive = dto.IsActive
            };
            return await _tableRepository.AddAsync(table);
        }

        public async Task<List<Table>> GetTablesAsync()
        {
            return await _tableRepository.GetAllAsync() ?? new List<Table>();
        }

        public async Task UpdateTableAsync(int id, CreateTableDto dto)
        {
            if (dto.Number <= 0)
                throw new ArgumentException("Table number must be greater than zero.");

            var existing = await _tableRepository.GetAllAsync() ?? new List<Table>();
            if (existing.Any(t => t.Number == dto.Number && t.Id != id && !t.IsDeleted))
                throw new InvalidOperationException($"Table number {dto.Number} is already in use.");

            var table = await _tableRepository.GetByIdAsync(id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{id} not found.");

            table.Number = dto.Number;
            table.Name = dto.Name;
            table.Capacity = dto.Capacity;
            table.IsActive = dto.IsActive;
            await _tableRepository.UpdateAsync(table);
        }

        public async Task DeleteTableAsync(int id)
        {
            var table = await _tableRepository.GetByIdAsync(id);
            if (table is null || table.IsDeleted)
                throw new KeyNotFoundException($"Table #{id} not found or already deleted.");

            await _tableRepository.DeleteAsync(id);
        }
    }
}
