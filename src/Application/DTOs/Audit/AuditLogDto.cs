using System;

namespace HotelPOS.Application.DTOs.Audit
{
    public class AuditLogDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
        public string? Username { get; set; }
    }
}
