using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class CashSession
    {
        [NotMapped]
        public int SNo { get; set; }

        public int Id { get; set; }

        [Required]
        public DateTime OpenedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal? ClosingBalance { get; set; } // Theoretical closing balance (Opening + Sales)

        public decimal? ActualCash { get; set; }    // Physical cash counted

        [MaxLength(100)]
        public string OpenedBy { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ClosedBy { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open, Closed

        public string? Notes { get; set; }
    }
}
