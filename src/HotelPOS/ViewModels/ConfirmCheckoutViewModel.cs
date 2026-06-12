using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        private string _paymentMode = "Cash";

        [ObservableProperty]
        private decimal _cashAmount;

        [ObservableProperty]
        private decimal _cardAmount;

        [ObservableProperty]
        private decimal _upiAmount;

        public decimal OutstandingBalance => Math.Max(0, FinalPayableAmount - (CashAmount + CardAmount + UpiAmount));

        public bool IsSplitPayment => PaymentMode == "Split";

        public bool CanConfirm => !IsSplitPayment || OutstandingBalance == 0;

        partial void OnCashAmountChanged(decimal value) => OnPaymentAmountChanged();
        partial void OnCardAmountChanged(decimal value) => OnPaymentAmountChanged();
        partial void OnUpiAmountChanged(decimal value) => OnPaymentAmountChanged();
        partial void OnPaymentModeChanged(string value)
        {
            if (value != "Split")
            {
                CashAmount = 0;
                CardAmount = 0;
                UpiAmount = 0;
            }
            else
            {
                CashAmount = FinalPayableAmount;
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
