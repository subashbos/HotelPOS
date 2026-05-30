using HotelPOS.Domain;

namespace HotelPOS.Application.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<(bool Success, string Error)> AddUserAsync(string username, string password, string role, int roleId);
        Task ToggleActiveAsync(int userId, bool isActive);
        Task DeleteUserAsync(int userId, int currentUserId);
        Task<(bool Success, string Error)> ResetPasswordAsync(int userId, string newPassword);
    }
}
