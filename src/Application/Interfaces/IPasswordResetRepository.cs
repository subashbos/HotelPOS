using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IPasswordResetRepository
    {
        Task AddAsync(PasswordResetRequest request);
        Task<PasswordResetRequest?> GetLatestActiveAsync(int userId);
        Task UpdateAsync(PasswordResetRequest request);
    }
}
