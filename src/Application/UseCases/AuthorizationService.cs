using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserContext _userContext;

        public AuthorizationService(IUserContext userContext)
        {
            _userContext = userContext;
        }

        public bool HasPermission(string moduleName)
        {
            if (!_userContext.IsAuthenticated)
                return false;

            var permissions = _userContext.Permissions;
            if (permissions != null && permissions.Count > 0)
            {
                var perm = permissions.FirstOrDefault(p =>
                    string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
                if (perm != null)
                    return perm.CanAccess;
            }

            var role = _userContext.CurrentRole;
            if (string.IsNullOrWhiteSpace(role))
                return false;

            if (string.Equals(role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(role, RoleNames.Cashier, StringComparison.OrdinalIgnoreCase))
                return moduleName is PermissionModules.Billing or PermissionModules.Shift;

            return false;
        }

        public void EnsurePermission(string moduleName)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Authentication is required.");

            if (!HasPermission(moduleName))
                throw new UnauthorizedAccessException($"Access denied for module '{moduleName}'.");
        }

        public void EnsureSelfOrPermission(int targetUserId, string moduleName)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Authentication is required.");

            if (_userContext.CurrentUserId == targetUserId)
                return;

            EnsurePermission(moduleName);
        }
    }
}
