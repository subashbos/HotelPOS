using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync(bool includeInactive = false);
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByPhoneAsync(string phone);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeactivateAsync(int id);
        Task<bool> ExistsByPhoneAsync(string phone, int excludeId = 0);
    }
}
