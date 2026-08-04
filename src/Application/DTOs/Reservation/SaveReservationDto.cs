namespace HotelPOS.Application.DTOs.Reservation
{
    /// <summary>DTO used for booking a new table reservation via API / ViewModel.</summary>
    public class SaveReservationDto
    {
        public int TableId { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int PartySize { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Body for the dedicated status-transition endpoint.</summary>
    public class ChangeReservationStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
