using HotelPOS.Domain;

namespace HotelPOS.Application.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        (string Hash, string Salt) HashPassword(string password);
    }
}
