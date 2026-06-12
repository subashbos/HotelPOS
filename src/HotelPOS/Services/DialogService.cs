using HotelPOS.Application.Interfaces;
using HotelPOS.ViewModels;
using HotelPOS.Views;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HotelPOS.Services
{
    public class DialogService : IDialogService
    {
        public Task<bool> ShowConfirmCheckoutAsync(ConfirmCheckoutDetails details)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Ensure window creation and showing happens on the UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new ConfirmCheckoutViewModel
                {
                    TotalItems = details.TotalItems,
                    TotalAmount = details.TotalAmount,
                    DiscountAmount = details.DiscountAmount,
                    FinalPayableAmount = details.FinalPayableAmount,
                    PaymentMode = details.PaymentMode
                };

                var window = new ConfirmCheckoutWindow(viewModel);

                // Set active window as owner so it centers properly over the parent window
                var activeWindow = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                                   ?? System.Windows.Application.Current.MainWindow;
                
                if (activeWindow != null && activeWindow != window)
                {
                    window.Owner = activeWindow;
                }

                var result = window.ShowDialog();
                if (result == true)
                {
                    details.PaymentMode = viewModel.PaymentMode;
                    details.CashAmount = viewModel.CashAmount;
                    details.CardAmount = viewModel.CardAmount;
                    details.UpiAmount = viewModel.UpiAmount;
                }
                tcs.SetResult(result ?? false);
            });

            return tcs.Task;
        }
    }
}
