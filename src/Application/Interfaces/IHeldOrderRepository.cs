using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public interface IHeldOrderRepository
    {
        Task SaveAsync(HeldOrder held);
        Task DeleteAsync(Guid id);
        Task ClearAllAsync();
        Task<List<HeldOrder>> GetAllAsync();
    }
}
