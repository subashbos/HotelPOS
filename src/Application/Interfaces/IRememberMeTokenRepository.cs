using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IRememberMeTokenRepository
    {
        Task AddAsync(RememberMeToken token);
        Task<RememberMeToken?> GetByHashAsync(string tokenHash);
        Task UpdateAsync(RememberMeToken token);
    }
}
