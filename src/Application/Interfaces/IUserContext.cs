using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.Interfaces
{
    public interface IUserContext
    {
        bool IsAuthenticated { get; }
        int? CurrentUserId { get; }
        string? CurrentUsername { get; }
        string? CurrentRole { get; }
        IReadOnlyList<RolePermission>? Permissions { get; }
    }
}
