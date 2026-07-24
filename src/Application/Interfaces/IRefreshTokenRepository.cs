using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken?> GetByHashAsync(string tokenHash);
        Task UpdateAsync(RefreshToken token);

        /// <summary>Revokes every not-yet-revoked token in the family - the response to detecting reuse of an already-rotated token (likely theft).</summary>
        Task RevokeFamilyAsync(Guid familyId, DateTime revokedUtc);
    }
}
