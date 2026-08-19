namespace HotelPOS.Application.Interfaces
{
    public interface IAuthorizationService
    {
        bool HasPermission(string moduleName);
        void EnsurePermission(string moduleName);
        void EnsureSelfOrPermission(int targetUserId, string moduleName);

        /// <summary>Like <see cref="EnsurePermission"/>, but additionally requires the role's
        /// per-module <c>CanEdit</c> flag - use for create/update/void/refund and similar
        /// mutating-but-not-destructive actions.</summary>
        void EnsureEditPermission(string moduleName);

        /// <summary>Like <see cref="EnsurePermission"/>, but additionally requires the role's
        /// per-module <c>CanDelete</c> flag - use for delete actions specifically.</summary>
        void EnsureDeletePermission(string moduleName);
    }
}
