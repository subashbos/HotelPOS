using CommunityToolkit.Mvvm.ComponentModel;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using System.Collections.ObjectModel;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        private readonly ICartService _cartService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly IDialogService? _dialogService;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _selectedCategoryId = 0;

        [ObservableProperty]
        private decimal _totalAmount;

        [ObservableProperty]
        private decimal _gstAmount;

        [ObservableProperty]
        private decimal _subtotal;

        [ObservableProperty]
        private int _tableNumber = 1;

        private int _lastDineInTable = 1; // Remember the last real table for DineIn swaps

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private decimal _discountAmount;

        [ObservableProperty]
        private string _paymentMode = PaymentModes.Cash;

        [ObservableProperty]
        private string _orderType = OrderTypes.DineIn;

        // Customer Details
        [ObservableProperty]
        private string? _customerName;

        [ObservableProperty]
        private string? _customerPhone;

        [ObservableProperty]
        private string? _customerGstin;

        public ObservableCollection<Item> Items { get; } = new();
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<CartRow> Cart { get; } = new();
        public ObservableCollection<int> ActiveTabs { get; } = new();

        /// <summary>True when the current order type does not require a table (Takeaway or Online).</summary>
        public bool IsTableless => OrderType == OrderTypes.Takeaway || OrderType == OrderTypes.Online; // NOSONAR

        partial void OnOrderTypeChanged(string value)
        {
            if (IsTableless)
            {
                if (TableNumber > 0) _lastDineInTable = TableNumber;
                _tableNumber = 0;
                OnPropertyChanged(nameof(TableNumber));
                UpdateCart();
            }
            else if (TableNumber == 0)
            {
                TableNumber = _lastDineInTable;
            }

            OnPropertyChanged(nameof(IsTableless));
            OnPropertyChanged(nameof(IsTableVisible));
        }

        /// <summary>Drives visibility of the table strip and table-related controls.</summary>
        public bool IsTableVisible => !IsTableless;

        [ObservableProperty]
        private bool _isTransferMode;

        [ObservableProperty]
        private bool _isCompositionScheme;

        [ObservableProperty]
        private CartRow? _selectedCartRow;

        private int _selectedQty;
        public int SelectedQty
        {
            get => _selectedQty;
            set
            {
                if (value < 1)
                {
                    StatusMessage = "Quantity cannot be less than 1.";
                    value = 1;
                }

                if (SelectedCartRow != null)
                {
                    var item = _allItems.FirstOrDefault(i => i.Id == SelectedCartRow.ItemId);
                    if (item != null && item.TrackInventory && value > item.StockQuantity)
                    {
                        StatusMessage = $"⚠ Out of stock: {item.Name} (Avail: {item.StockQuantity})";
                        value = item.StockQuantity; // Cap at max available
                        if (value < 1) value = 1; // Minimum 1 even if 0 stock (allow billing if user insists, or handle strictly)
                        // Actually, if track inventory is ON and stock is 0, we should maybe prevent adding?
                        // The user said "Eliminate critical business loopholes". So let's be strict.
                        if (item.StockQuantity <= 0)
                        {
                            StatusMessage = $"🚫 CANNOT ADD: {item.Name} is out of stock.";
                            // Revert to old value or keep at current? 
                            // If we revert, we need to notify UI.
                            OnPropertyChanged(nameof(SelectedQty));
                            return;
                        }
                    }
                }

                if (SetProperty(ref _selectedQty, value) && SelectedCartRow != null)
                {
                    _cartService.SetQuantity(TableNumber, SelectedCartRow.ItemId, value);
                    UpdateCart();
                }
            }
        }

        partial void OnSelectedCartRowChanged(CartRow? value)
        {
            if (value != null)
                _selectedQty = value.Quantity;
            else
                _selectedQty = 0;
            OnPropertyChanged(nameof(SelectedQty));
        }

        /// <summary>
        /// Raised after a successful order update so the shell can navigate
        /// back to the dashboard and refresh it.
        /// </summary>
        public event Action? OrderUpdated;
        public event Action? OrderEditCancelled;
        public event Action? PrintPreviewClosed;
        public event Action? CartCleared;
        public event Action? CheckoutCancelled;

        private List<Item> _allItems = new();

        public BillingViewModel(ICartService cartService, ISettingService settingService,
                                INotificationService notificationService, IDialogService? dialogService = null)
        {
            _cartService = cartService;
            _settingService = settingService;
            _notificationService = notificationService;
            _dialogService = dialogService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(cartService);
                App.RegisterTestService(settingService);
                App.RegisterTestService(notificationService);
                if (dialogService != null) App.RegisterTestService(dialogService);
            }

            LoadHeldOrders();
        }

        /// <summary>
        /// Test-only helper: registers the collaborator services this view model resolves via scoped DI
        /// lookups (rather than constructor injection) so unit tests can inject mocks for them.
        /// </summary>
        public static void RegisterTestServices(IItemService itemService, IOrderService orderService,
            ICategoryService categoryService, ICashService cashService, ITableService tableService)
        {
            App.RegisterTestService(itemService);
            App.RegisterTestService(orderService);
            App.RegisterTestService(categoryService);
            App.RegisterTestService(cashService);
            App.RegisterTestService(tableService);
        }

        [ObservableProperty]
        private ObservableCollection<HeldOrder> _heldOrders = new();

        [ObservableProperty]
        private bool _isHeldOrdersPopupOpen;

        [ObservableProperty]
        private bool _isEditMode;
    }
}
