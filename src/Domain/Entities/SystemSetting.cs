using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        // ── Hotel Profile ─────────────────────────────────────────────────────
        public string HotelName { get; set; } = "Hotel POS";
        public string HotelAddress { get; set; } = string.Empty;
        public string HotelPhone { get; set; } = string.Empty;

        /// <summary>GSTIN — mandatory for Indian GST-compliant billing.</summary>
        public string HotelGst { get; set; } = string.Empty;

        // ── Printer & Hardware ────────────────────────────────────────────────
        public string DefaultPrinter { get; set; } = "Microsoft Print to PDF";
        public bool ShowPrintPreview { get; set; } = true;

        /// <summary>"Thermal" (80mm) or "A4"</summary>
        public string ReceiptFormat { get; set; } = "Thermal";

        // ── Receipt Display Toggles ───────────────────────────────────────────
        public bool ShowGstBreakdown { get; set; } = true;
        public bool ShowItemsOnBill { get; set; } = true;
        public bool ShowDiscountLine { get; set; } = false;
        public bool ShowPhoneOnReceipt { get; set; } = true;
        public bool ShowThankYouFooter { get; set; } = true;

        // ── Billing Options ───────────────────────────────────────────────────
        /// <summary>When true, grand total is rounded to the nearest rupee; round-off ± shown on bill.</summary>
        public bool EnableRoundOff { get; set; } = false;

        /// <summary>When true, follows Indian GST Composition Scheme (5% turnover tax, no collection from customer, Bill of Supply title).</summary>
        public bool IsCompositionScheme { get; set; } = false;

        // ── Disaster Recovery ────────────────────────────────────────────────
        public bool EnableAutomatedBackups { get; set; } = true;
        public string? OffsiteBackupPath { get; set; }

        // ── Session Security ──────────────────────────────────────────────────
        /// <summary>Minutes of inactivity before the WPF session auto-logs-out. 0 disables the timeout.</summary>
        public int IdleTimeoutMinutes { get; set; } = 15;

        // ── Outgoing Email (SMTP) — used for the self-service "forgot password" flow ──
        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public bool SmtpUseSsl { get; set; } = true;
        public string? SmtpFromAddress { get; set; }

        /// <summary>Copies all editable fields from <paramref name="source"/> onto this instance (Id is left untouched).</summary>
        public void UpdateFrom(SystemSetting source)
        {
            HotelName = source.HotelName;
            HotelAddress = source.HotelAddress;
            HotelPhone = source.HotelPhone;
            HotelGst = source.HotelGst;
            DefaultPrinter = source.DefaultPrinter;
            ShowPrintPreview = source.ShowPrintPreview;
            ReceiptFormat = source.ReceiptFormat;
            ShowGstBreakdown = source.ShowGstBreakdown;
            ShowItemsOnBill = source.ShowItemsOnBill;
            ShowDiscountLine = source.ShowDiscountLine;
            ShowPhoneOnReceipt = source.ShowPhoneOnReceipt;
            ShowThankYouFooter = source.ShowThankYouFooter;
            EnableRoundOff = source.EnableRoundOff;
            IsCompositionScheme = source.IsCompositionScheme;
            EnableAutomatedBackups = source.EnableAutomatedBackups;
            OffsiteBackupPath = source.OffsiteBackupPath;
            IdleTimeoutMinutes = source.IdleTimeoutMinutes;
            SmtpHost = source.SmtpHost;
            SmtpPort = source.SmtpPort;
            SmtpUsername = source.SmtpUsername;
            SmtpPassword = source.SmtpPassword;
            SmtpUseSsl = source.SmtpUseSsl;
            SmtpFromAddress = source.SmtpFromAddress;
        }
    }
}
