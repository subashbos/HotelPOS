namespace HotelPOS.Application.Interfaces
{
    public interface IBackupService
    {
        Task CreateBackupAsync(string? customPath = null);
    }
}
