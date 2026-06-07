using HotelPOS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
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

        public string? CurrentUsername => 
            _httpContextAccessor.HttpContext?.User?.Identity?.Name 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
    }
}
