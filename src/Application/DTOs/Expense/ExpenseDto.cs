namespace HotelPOS.Application.DTOs.Expense
{
    /// <summary>Read model for an Expense.</summary>
    public class ExpenseDto
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? PaymentMode { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByUsername { get; set; }
    }
}
