using HotelPOS.Domain;

namespace HotelPOS.Application.Interface
{
    public interface ITableService
    {
        Task<int> AddTableAsync(CreateTableDto dto);
        Task<List<Table>> GetTablesAsync();
        Task UpdateTableAsync(int id, CreateTableDto dto);
        Task DeleteTableAsync(int id);
    }
}
