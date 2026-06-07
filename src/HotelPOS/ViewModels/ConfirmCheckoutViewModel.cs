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

        public bool DialogResult { get; private set; }

        [RelayCommand]
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
