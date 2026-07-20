namespace HotelPOS.Application.Interfaces
{
    public interface IPasswordResetService
    {
        /// <summary>Sends a reset code by email if the account exists and has an email on file. Never reveals whether it does.</summary>
        Task RequestResetAsync(string username);

        Task<(bool Success, string? Error)> ConfirmResetAsync(string username, string code, string newPassword);
    }
}
