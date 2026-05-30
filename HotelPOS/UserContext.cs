using HotelPOS.Domain.Interfaces;

namespace HotelPOS
{
    public class UserContext : IUserContext
    {
        public string? CurrentUsername => AppSession.CurrentUser?.Username;
    }
}
