namespace HotelPOS.Application.DTOs.Supplier
{
    /// <summary>DTO used for creating or updating a Supplier via API / ViewModel.</summary>
    public class SaveSupplierDto
    {
        public int Id { get; set; }          // 0 = new, >0 = update
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gstin { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Pincode { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public string? PaymentTerms { get; set; }
    }
}
