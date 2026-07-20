using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ModuleName { get; set; } = string.Empty;

        public bool CanAccess { get; set; } = true;
        
        // Future proofing for more granular control
        public bool CanEdit { get; set; } = true;
        public bool CanDelete { get; set; } = true;
    }
}
