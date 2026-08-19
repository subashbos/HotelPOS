using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User?> AuthenticateInternalAsync(string username, string password);
        (string Hash, string Salt) HashPassword(string password);

        Task<bool> IsTwoFactorLockedOutAsync(string username);
        Task RegisterFailedTwoFactorAttemptAsync(string username);
        Task ClearTwoFactorLockoutAsync(string username);
    }
}
