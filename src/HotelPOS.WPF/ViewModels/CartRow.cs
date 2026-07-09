using CommunityToolkit.Mvvm.ComponentModel;

namespace HotelPOS.ViewModels
{
    /// <summary>Cart display row with serial number and ItemId for cart operations.</summary>
    public partial class CartRow : ObservableObject
    {
        [ObservableProperty]
        private int _sNo;

        [ObservableProperty]
        private int _itemId;

        [ObservableProperty]
        private string _itemName = string.Empty;

        [ObservableProperty]
        private int _quantity;

        [ObservableProperty]
        private decimal _price;

        [ObservableProperty]
        private decimal _taxPercentage;

        [ObservableProperty]
        private decimal _taxAmount;

        [ObservableProperty]
        private decimal _total;
    }
}
