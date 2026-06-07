using HotelPOS.Application.DTOs.Table;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ITableService
    {
        Task<int> AddTableAsync(CreateTableDto dto);
        Task<List<Table>> GetTablesAsync();
        Task UpdateTableAsync(int id, CreateTableDto dto);
        Task DeleteTableAsync(int id);
    }
}
