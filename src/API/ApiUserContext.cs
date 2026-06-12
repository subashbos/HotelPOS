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

        public ApiUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
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

        public IReadOnlyList<RolePermission>? Permissions => null;
    }
}
