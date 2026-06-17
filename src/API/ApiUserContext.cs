using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Http;
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
                if (!_permissionsLoaded)
                {
                    var username = CurrentUsername;
                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = Task.Run(async () => await _userRepository.GetUserByUsernameAsync(username)).GetAwaiter().GetResult();
                        _cachedPermissions = user?.RoleDetails?.Permissions;
                    }
                    _permissionsLoaded = true;
                }
                return _cachedPermissions;
            }
        }
    }
}
