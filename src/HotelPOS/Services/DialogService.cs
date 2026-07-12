using HotelPOS.Application.Interfaces;
using HotelPOS.ViewModels;
using HotelPOS.Views;
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
                if (result.GetValueOrDefault())
                {
                    details.PaymentMode = viewModel.PaymentMode;
                    details.CashAmount = viewModel.ParsedCash;
                    details.CardAmount = viewModel.ParsedCard;
                    details.UpiAmount = viewModel.ParsedUpi;
                }
                tcs.SetResult(result ?? false);
            });

            return tcs.Task;
        }

        public Task<DialogResult> ShowMessageAsync(string message, string title, DialogButton button, DialogIcon icon)
        {
            var tcs = new TaskCompletionSource<DialogResult>();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new CustomMessageBoxViewModel();
                viewModel.Setup(message, title, button, icon);

                var window = new CustomMessageBoxWindow(viewModel);

                var activeWindow = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                                   ?? System.Windows.Application.Current.MainWindow;

                if (activeWindow != null && activeWindow != window)
                {
                    window.Owner = activeWindow;
                }

                window.ShowDialog();
                tcs.SetResult(viewModel.Result);
            });

            return tcs.Task;
        }

        public DialogResult ShowMessage(string message, string title, DialogButton button, DialogIcon icon)
        {
            DialogResult result = DialogResult.None;
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var viewModel = new CustomMessageBoxViewModel();
                viewModel.Setup(message, title, button, icon);

                var window = new CustomMessageBoxWindow(viewModel);

                var activeWindow = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                                   ?? System.Windows.Application.Current.MainWindow;

                if (activeWindow != null && activeWindow != window)
                {
                    window.Owner = activeWindow;
                }

                window.ShowDialog();
                result = viewModel.Result;
            });

            return result;
        }
    }
}
