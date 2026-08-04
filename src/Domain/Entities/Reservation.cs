using System;
using System.ComponentModel.DataAnnotations;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Domain.Entities
{
    /// <summary>A future-dated booking of a dining <see cref="Table"/> for a given date/time window,
    /// distinct from the live table-status tracking used during active billing.</summary>
    public class Reservation
    {
        public int Id { get; set; }

        public int TableId { get; set; }
        public Table? Table { get; set; }

        /// <summary>Optional link to a CRM <see cref="Customer"/> profile. Null for a prospect without a saved profile.</summary>
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [MaxLength(150)]
        public string? CustomerName { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        public DateTime ReservationDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public int PartySize { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = ReservationStatuses.Reserved;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
