using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken?> GetByHashAsync(string tokenHash);
        Task UpdateAsync(RefreshToken token);
    }
}
