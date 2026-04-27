using HotelPOS.Domain.Interface;

namespace HotelPOS
{
    public class UserContext : IUserContext
    {
        public string? CurrentUsername => AppSession.CurrentUser?.Username;
    }
}
