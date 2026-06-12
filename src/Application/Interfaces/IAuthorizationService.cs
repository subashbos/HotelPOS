namespace HotelPOS.Application.Interfaces
{
    public interface IAuthorizationService
    {
        bool HasPermission(string moduleName);
        void EnsurePermission(string moduleName);
        void EnsureSelfOrPermission(int targetUserId, string moduleName);
    }
}
