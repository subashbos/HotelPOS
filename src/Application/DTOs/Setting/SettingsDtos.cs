namespace HotelPOS.Application.DTOs.Setting
{
    /// <summary>Read model for system settings. Never exposes the raw SMTP password.</summary>
    public class SettingsDto
    {
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string HotelPhone { get; set; } = string.Empty;
        public string HotelGst { get; set; } = string.Empty;

        public string DefaultPrinter { get; set; } = string.Empty;
        public bool ShowPrintPreview { get; set; }
        public string ReceiptFormat { get; set; } = string.Empty;

        public bool ShowGstBreakdown { get; set; }
        public bool ShowItemsOnBill { get; set; }
        public bool ShowDiscountLine { get; set; }
        public bool ShowPhoneOnReceipt { get; set; }
        public bool ShowThankYouFooter { get; set; }

        public bool EnableRoundOff { get; set; }
        public bool IsCompositionScheme { get; set; }

        public bool EnableAutomatedBackups { get; set; }
        public string? OffsiteBackupPath { get; set; }

        public int IdleTimeoutMinutes { get; set; }

        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string? SmtpUsername { get; set; }
        public bool SmtpPasswordSet { get; set; }
        public bool SmtpUseSsl { get; set; }
        public string? SmtpFromAddress { get; set; }
    }

    /// <summary>DTO used to save system settings. SmtpPassword is left null/empty to keep the existing password.</summary>
    public class SaveSettingsDto
    {
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string HotelPhone { get; set; } = string.Empty;
        public string HotelGst { get; set; } = string.Empty;

        public string DefaultPrinter { get; set; } = string.Empty;
        public bool ShowPrintPreview { get; set; }
        public string ReceiptFormat { get; set; } = string.Empty;

        public bool ShowGstBreakdown { get; set; }
        public bool ShowItemsOnBill { get; set; }
        public bool ShowDiscountLine { get; set; }
        public bool ShowPhoneOnReceipt { get; set; }
        public bool ShowThankYouFooter { get; set; }

        public bool EnableRoundOff { get; set; }
        public bool IsCompositionScheme { get; set; }

        public bool EnableAutomatedBackups { get; set; }
        public string? OffsiteBackupPath { get; set; }

        public int IdleTimeoutMinutes { get; set; }

        public string? SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string? SmtpUsername { get; set; }

        /// <summary>Null/empty means "leave the current password unchanged".</summary>
        public string? SmtpPassword { get; set; }
        public bool SmtpUseSsl { get; set; }
        public string? SmtpFromAddress { get; set; }
    }
}
