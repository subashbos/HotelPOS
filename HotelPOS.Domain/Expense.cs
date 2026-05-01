using System;
using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "General"; // e.g., Salary, Rent, Raw Material, Utilities

        [MaxLength(50)]
        public string? PaymentMode { get; set; } = "Cash";

        public int? CreatedBy { get; set; }
        public User? User { get; set; }
    }
}
