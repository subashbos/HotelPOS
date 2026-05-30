using System.ComponentModel.DataAnnotations;

namespace HotelPOS.Domain
{
    public class Supplier
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? ContactPerson { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        public string? Email { get; set; }
        
        [MaxLength(500)]
        public string? Address { get; set; }
        
        [MaxLength(20)]
        public string? Gstin { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? Pincode { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal CreditLimit { get; set; }

        [MaxLength(50)]
        public string? PaymentTerms { get; set; }
    }
}
