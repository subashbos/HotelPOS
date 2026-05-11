using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain
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
    }
}
