using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS
{
    public class UserContext : IUserContext
    {
        public bool IsAuthenticated => AppSession.IsLoggedIn;
        public int? CurrentUserId => AppSession.CurrentUser?.Id;
        public string? CurrentUsername => AppSession.CurrentUser?.Username;
        public string? CurrentRole => AppSession.CurrentUser?.Role;
        public IReadOnlyList<RolePermission>? Permissions => AppSession.CurrentUser?.RoleDetails?.Permissions;
    }
}
