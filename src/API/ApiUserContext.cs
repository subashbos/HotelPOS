using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace HotelPOS.Api
{
    public class ApiUserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserRepository _userRepository;
        private IReadOnlyList<RolePermission>? _cachedPermissions;
        private bool _permissionsLoaded;

        public ApiUserContext(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepository = userRepository;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public int? CurrentUserId
        {
            get
            {
                var sub = User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                return int.TryParse(sub, out var id) ? id : null;
            }
        }

        public string? CurrentUsername =>
            User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
            ?? User?.Identity?.Name;

        public string? CurrentRole => User?.FindFirst(ClaimTypes.Role)?.Value;

        public IReadOnlyList<RolePermission>? Permissions
        {
            get
            {
                // PermissionsPreloadMiddleware awaits EnsurePermissionsLoadedAsync() early in the
                // pipeline for every authenticated request, so this synchronous path is normally
                // already-cached data. The blocking fallback only exists for callers outside the
                // HTTP pipeline (e.g. constructing this type directly in a test).
                if (!_permissionsLoaded)
                {
                    EnsurePermissionsLoadedAsync().GetAwaiter().GetResult();
                }
                return _cachedPermissions;
            }
        }

        /// <summary>
        /// Loads and caches the current user's permissions. Idempotent - safe to call once per
        /// request from PermissionsPreloadMiddleware regardless of whether anything has already
        /// read <see cref="Permissions"/>.
        /// </summary>
        public async Task EnsurePermissionsLoadedAsync()
        {
            if (_permissionsLoaded) return;

            var username = CurrentUsername;
            if (!string.IsNullOrEmpty(username))
            {
                var user = await _userRepository.GetUserByUsernameAsync(username);
                _cachedPermissions = user?.RoleDetails?.Permissions;
            }
            _permissionsLoaded = true;
        }
    }
}
