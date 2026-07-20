using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IRememberMeService
    {
        Task<string> IssueTokenAsync(int userId);

        /// <summary>Validates and single-use-consumes a remember-me token; call IssueTokenAsync again to keep the session rolling.</summary>
        Task<User?> ValidateAndConsumeAsync(string username, string rawToken);

        Task RevokeAsync(string rawToken);
    }
}
