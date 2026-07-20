using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface ILoginLockoutRepository
    {
        Task<LoginLockout?> GetAsync(string normalizedUsername);
        Task SaveAsync(LoginLockout lockout);
        Task ClearAsync(string normalizedUsername);
    }
}
