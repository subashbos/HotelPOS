using System.Text.RegularExpressions;

namespace HotelPOS.ViewModels
{
    /// <summary>
    /// Shared field-validation rules used by the various entry-dialog view models
    /// (customer, supplier, employee, ...), so the same rule isn't reimplemented per screen.
    /// </summary>
    public static class EntryValidation
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, TimeSpan.FromSeconds(1));

        public static bool ValidateRequired(string? value, string fieldName, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"{fieldName} is required";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public static bool ValidatePhone(string? phone, out string error)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                error = string.Empty;
                return true;
            }
            var cleanPhone = Regex.Replace(phone, @"[^\d]", "", RegexOptions.None, TimeSpan.FromMilliseconds(250));
            if (cleanPhone.Length < 10 || cleanPhone.Length > 15)
            {
                error = "Invalid phone number (must be 10-15 digits)";
                return false;
            }
            error = string.Empty;
            return true;
        }

        public static bool ValidateEmail(string? email, out string error)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                error = string.Empty;
                return true;
            }
            if (!EmailRegex.IsMatch(email.Trim()))
            {
                error = "Please enter a valid Email ID";
                return false;
            }
            error = string.Empty;
            return true;
        }
    }
}
