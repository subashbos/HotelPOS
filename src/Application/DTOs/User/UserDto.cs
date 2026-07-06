using System.Collections.Generic;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Application.DTOs.User
{
    public class UserDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = RoleNames.Cashier;
        public int? RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool MustChangePassword { get; set; } = false;
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
