namespace HotelPOS.Domain.Interface
{
    public interface ITableRepository
    {
        Task<List<Table>> GetAllAsync();
        Task<Table?> GetByIdAsync(int id);
        Task<int> AddAsync(Table table);
        Task UpdateAsync(Table table);
        Task DeleteAsync(int id);
    }
}
