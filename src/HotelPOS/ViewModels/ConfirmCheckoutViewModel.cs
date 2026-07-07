using HotelPOS.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Domain.Common.Constants;
using System.Windows;

namespace HotelPOS.ViewModels
{
    public partial class ConfirmCheckoutViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _totalItems;

        [ObservableProperty]
        private decimal _totalAmount;

        [ObservableProperty]
        private decimal _discountAmount;

        [ObservableProperty]
        private decimal _finalPayableAmount;

        [ObservableProperty]
        private string _paymentMode = PaymentModes.Cash;

        [ObservableProperty]
        private string _cashAmount = "0";

        [ObservableProperty]
        private string _cardAmount = "0";

        [ObservableProperty]
        private string _upiAmount = "0";

        public decimal ParsedCash => decimal.TryParse(CashAmount, out var v) ? v : 0;
        public decimal ParsedCard => decimal.TryParse(CardAmount, out var v) ? v : 0;
        public decimal ParsedUpi => decimal.TryParse(UpiAmount, out var v) ? v : 0;

        public decimal OutstandingBalance => Math.Max(0, FinalPayableAmount - (ParsedCash + ParsedCard + ParsedUpi));

        public bool IsSplitPayment => PaymentMode == "Split";

        public bool CanConfirm => !IsSplitPayment || OutstandingBalance == 0;

        partial void OnCashAmountChanged(string value) => OnPaymentAmountChanged();
        partial void OnCardAmountChanged(string value) => OnPaymentAmountChanged();
        partial void OnUpiAmountChanged(string value) => OnPaymentAmountChanged();
        partial void OnPaymentModeChanged(string value)
        {
            if (value != "Split")
            {
                CashAmount = "0";
                CardAmount = "0";
                UpiAmount = "0";
            }
            else
            {
                CashAmount = FinalPayableAmount.ToString("0.##");
            }
            OnPropertyChanged(nameof(IsSplitPayment));
            OnPropertyChanged(nameof(CanConfirm));
        }

        private void OnPaymentAmountChanged()
        {
            OnPropertyChanged(nameof(OutstandingBalance));
            OnPropertyChanged(nameof(CanConfirm));
            ConfirmCommand.NotifyCanExecuteChanged();
        }

        public bool DialogResult { get; private set; }

        [RelayCommand(CanExecute = nameof(CanConfirm))]
        private void Confirm(object? windowObj)
        {
            DialogResult = true;
            if (windowObj is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        [RelayCommand]
        private void Cancel(object? windowObj)
        {
            DialogResult = false;
            if (windowObj is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}
