using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain.Entities
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<RolePermission> Permissions { get; set; } = new();
    }
}
