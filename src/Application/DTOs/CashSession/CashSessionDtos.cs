namespace HotelPOS.Application.DTOs.CashSession
{
    /// <summary>Read model for a cash session (shift).</summary>
    public class CashSessionDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? ActualCash { get; set; }
        public string OpenedBy { get; set; } = string.Empty;
        public string? ClosedBy { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    /// <summary>DTO for opening a cash session (shift).</summary>
    public class OpenSessionDto
    {
        public decimal OpeningBalance { get; set; }
        public string OpenedBy { get; set; } = string.Empty;
    }

    /// <summary>DTO for closing a cash session (shift).</summary>
    public class CloseSessionDto
    {
        public decimal ActualCash { get; set; }
        public string? Notes { get; set; }
        public string ClosedBy { get; set; } = string.Empty;
    }
}
