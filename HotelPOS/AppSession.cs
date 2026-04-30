using HotelPOS.Domain;

namespace HotelPOS
{
    public static class AppSession
    {
        public static User? CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsManager => CurrentUser?.Role == "Manager" || IsAdmin;

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
