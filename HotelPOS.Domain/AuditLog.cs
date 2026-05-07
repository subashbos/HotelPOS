using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class AuditLog
    {
        [NotMapped]
        public int SNo { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public int EntityId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete

        [Required]
        public DateTime Timestamp { get; set; }

        public string? Details { get; set; }

        public string? Username { get; set; }
    }
}
