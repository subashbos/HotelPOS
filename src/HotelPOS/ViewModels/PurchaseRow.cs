using CommunityToolkit.Mvvm.ComponentModel;

namespace HotelPOS.ViewModels
{
    public partial class PurchaseRow : ObservableObject
    {
        [ObservableProperty]
        private int _sNo;

        [ObservableProperty]
        private int _itemId;

        [ObservableProperty]
        private string _itemName = string.Empty;

        [ObservableProperty]
        private int _quantity = 1;

        [ObservableProperty]
        private decimal _unitPrice;

        [ObservableProperty]
        private decimal _taxPercentage;

        [ObservableProperty]
        private decimal _discount;

        [ObservableProperty]
        private decimal _total;
    }
}
