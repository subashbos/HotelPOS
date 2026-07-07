using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    /// <summary>Persists failed-login counters so lockouts survive an app/service restart.</summary>
    public class LoginLockout
    {
        [Key]
        [MaxLength(50)]
        public string NormalizedUsername { get; set; } = string.Empty;

        public int FailedAttempts { get; set; }

        public DateTime? LockedUntilUtc { get; set; }

        public DateTime LastAttemptUtc { get; set; }
    }
}
