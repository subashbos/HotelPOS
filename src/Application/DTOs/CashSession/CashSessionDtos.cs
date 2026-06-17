namespace HotelPOS.Application.DTOs.CashSession
{
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
