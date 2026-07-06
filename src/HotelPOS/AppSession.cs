using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;

namespace HotelPOS
{
    public static class AppSession
    {
        private static readonly object _lock = new object();
        private static User? _currentUser;

        public static User? CurrentUser
        {
            get
            {
                lock (_lock)
                {
                    return _currentUser;
                }
            }
            set
            {
                lock (_lock)
                {
                    _currentUser = value;
                }
            }
        }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => string.Equals(CurrentUser?.Role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
        public static bool IsManager => string.Equals(CurrentUser?.Role, RoleNames.Manager, StringComparison.OrdinalIgnoreCase) || IsAdmin;

        public static void Logout()
        {
            lock (_lock)
            {
                _currentUser = null;
            }
        }
    }
}
