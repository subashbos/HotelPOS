using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    /// <summary>Opaque, rotating refresh token for the API JWT flow. Only the SHA-256 hash is persisted.</summary>
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresUtc { get; set; }

        public DateTime? RevokedUtc { get; set; }

        [MaxLength(64)]
        public string? ReplacedByTokenHash { get; set; }
    }
}
