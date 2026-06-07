using HotelPOS.Application.Interfaces;

namespace HotelPOS
{
    public class UserContext : IUserContext
    {
        public string? CurrentUsername => AppSession.CurrentUser?.Username;
    }
}
