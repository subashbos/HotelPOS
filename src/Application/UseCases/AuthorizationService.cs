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

        public bool HasPermission(string moduleName) => Check(moduleName, perm => perm.CanAccess);

        /// <summary>Requires CanAccess and CanEdit for an explicitly configured role permission.
        /// Roles with no explicit permissions fall back to the same role-name rule as
        /// <see cref="HasPermission"/> - that fallback predates CanEdit/CanDelete and has no
        /// finer-grained equivalent.</summary>
        public bool HasEditPermission(string moduleName) => Check(moduleName, perm => perm.CanAccess && perm.CanEdit);

        /// <summary>Requires CanAccess and CanDelete for an explicitly configured role permission.
        /// Roles with no explicit permissions fall back to the same role-name rule as
        /// <see cref="HasPermission"/> - that fallback predates CanEdit/CanDelete and has no
        /// finer-grained equivalent.</summary>
        public bool HasDeletePermission(string moduleName) => Check(moduleName, perm => perm.CanAccess && perm.CanDelete);

        private bool Check(string moduleName, Func<RolePermission, bool> predicate)
        {
            if (!_userContext.IsAuthenticated)
                return false;

            var permissions = _userContext.Permissions;
            if (permissions != null && permissions.Count > 0)
            {
                var perm = permissions.FirstOrDefault(p =>
                    string.Equals(p.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase));
                if (perm != null)
                    return predicate(perm);
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

        public void EnsureEditPermission(string moduleName)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Authentication is required.");

            if (!HasEditPermission(moduleName))
                throw new UnauthorizedAccessException($"Edit access denied for module '{moduleName}'.");
        }

        public void EnsureDeletePermission(string moduleName)
        {
            if (!_userContext.IsAuthenticated)
                throw new UnauthorizedAccessException("Authentication is required.");

            if (!HasDeletePermission(moduleName))
                throw new UnauthorizedAccessException($"Delete access denied for module '{moduleName}'.");
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
