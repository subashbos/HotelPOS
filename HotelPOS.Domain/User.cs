using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelPOS.Domain
{
    public class User
    {
        [NotMapped]
        public int SNo { get; set; }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Salt { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Cashier";

        /// <summary>Allows admins to disable a user account without deleting it.</summary>
        public bool IsActive { get; set; } = true;

        public bool MustChangePassword { get; set; } = false;
    }
}
