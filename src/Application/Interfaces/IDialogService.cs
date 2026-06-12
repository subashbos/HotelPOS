using System.Threading.Tasks;

namespace HotelPOS.Application.Interfaces
{
    public class ConfirmCheckoutDetails
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPayableAmount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public decimal CashAmount { get; set; }
        public decimal CardAmount { get; set; }
        public decimal UpiAmount { get; set; }
    }

    public interface IDialogService
    {
        Task<bool> ShowConfirmCheckoutAsync(ConfirmCheckoutDetails details);
    }
}
