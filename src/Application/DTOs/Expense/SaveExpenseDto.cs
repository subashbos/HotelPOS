namespace HotelPOS.Application.DTOs.Expense
{
    /// <summary>DTO used for creating or updating an Expense via the ViewModel/service layer.</summary>
    public class SaveExpenseDto
    {
        public int Id { get; set; }          // 0 = new, >0 = update
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = HotelPOS.Domain.Common.Constants.ExpenseCategories.General;
        public string? PaymentMode { get; set; }
        public int? CreatedBy { get; set; }
    }
}
