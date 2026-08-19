using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain.Entities
{
    /// <summary>A short-lived, emailed reset code for the self-service "forgot password" flow.</summary>
    public class PasswordResetRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(64)]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresUtc { get; set; }

        public bool Used { get; set; } = false;

        public int AttemptCount { get; set; } = 0;
    }
}
