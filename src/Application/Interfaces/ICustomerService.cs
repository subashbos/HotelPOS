using HotelPOS.Application.DTOs.Customer;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetCustomersAsync(bool includeInactive = false);
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer?> GetCustomerByPhoneAsync(string phone);
        Task SaveCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
        Task<CustomerHistoryDto> GetCustomerHistoryAsync(int id);
    }
}
