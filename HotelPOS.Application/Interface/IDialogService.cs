using System.Threading.Tasks;

namespace HotelPOS.Application.Interface
{
    public class ConfirmCheckoutDetails
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPayableAmount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
    }

    public interface IDialogService
    {
        Task<bool> ShowConfirmCheckoutAsync(ConfirmCheckoutDetails details);
    }
}
