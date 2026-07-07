using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HotelPOS.Services
{
    /// <summary>
    /// Persists a "remember me" credential locally, DPAPI-protected to the current Windows user account,
    /// so the WPF app can skip the login prompt on its next cold start.
    /// </summary>
    internal static class RememberMeStore
    {
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HotelPOS", "remember.token");

        public static void Save(string username, string rawToken)
        {
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);

            var payload = $"{username}\n{rawToken}";
            var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(payload), null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(FilePath, protectedBytes);
        }

        public static (string Username, string Token)? Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;

                var protectedBytes = File.ReadAllBytes(FilePath);
                var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                var payload = Encoding.UTF8.GetString(bytes);

                var parts = payload.Split('\n', 2);
                if (parts.Length != 2) return null;

                return (parts[0], parts[1]);
            }
            catch
            {
                return null;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
