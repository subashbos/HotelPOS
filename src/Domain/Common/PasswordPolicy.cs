using System.Linq;

namespace HotelPOS.Domain.Common
{
    /// <summary>Shared password-strength rule, applied both client-side (WPF dialogs) and server-side (validators).</summary>
    public static class PasswordPolicy
    {
        public const string RequirementsMessage =
            "Password must be at least 10 characters and include an uppercase letter, a lowercase letter, a digit, and a special character.";

        public static bool MeetsComplexityRequirements(string? password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            return password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}
