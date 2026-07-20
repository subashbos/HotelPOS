using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;

namespace HotelPOS.Application.Interfaces
{
    public class ConfirmCheckoutDetails
    {
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPayableAmount { get; set; }
        public string PaymentMode { get; set; } = PaymentModes.Cash;
        public decimal CashAmount { get; set; }
        public decimal CardAmount { get; set; }
        public decimal UpiAmount { get; set; }
    }

    public enum DialogButton { OK, OKCancel, YesNo, YesNoCancel }
    public enum DialogIcon { None, Information, Question, Warning, Error }
    public enum DialogResult { None, OK, Cancel, Yes, No }

    public interface IDialogService
    {
        Task<bool> ShowConfirmCheckoutAsync(ConfirmCheckoutDetails details);
        Task<DialogResult> ShowMessageAsync(string message, string title, DialogButton button, DialogIcon icon);
        DialogResult ShowMessage(string message, string title, DialogButton button, DialogIcon icon);
    }
}
