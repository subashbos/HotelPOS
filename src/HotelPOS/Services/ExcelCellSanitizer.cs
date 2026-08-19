#nullable enable

namespace HotelPOS.Services
{
    /// <summary>
    /// Guards against CSV/Excel formula injection in exported reports. Item/category/supplier/
    /// customer names are free text entered via the admin UI; if such a value starts with a
    /// character a spreadsheet application treats as a formula trigger (=, +, -, @, or a leading
    /// tab), opening the exported .xlsx can execute it as a formula (e.g. a HYPERLINK/DDE call
    /// exfiltrating data or launching a process) rather than displaying it as plain text.
    /// </summary>
    public static class ExcelCellSanitizer
    {
        private static readonly char[] FormulaTriggers = { '=', '+', '-', '@', '\t', '\r' };

        /// <summary>Prefixes a leading formula-trigger character with an apostrophe so spreadsheet
        /// applications always render the value as literal text, never as a formula.</summary>
        public static string ForSpreadsheet(this string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            return FormulaTriggers.Contains(value[0]) ? "'" + value : value;
        }
    }
}
