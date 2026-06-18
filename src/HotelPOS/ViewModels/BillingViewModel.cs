using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace HotelPOS.ViewModels
{
    public partial class BillingViewModel : ObservableObject
    {
        private readonly IItemService _itemService;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly ICategoryService _categoryService;
        private readonly INotificationService _notificationService;
        private readonly ICashService _cashService;
        private readonly ITableService _tableService;
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
        private string _paymentMode = "Cash";

        [ObservableProperty]
        private string _orderType = "DineIn";

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
        public bool IsTableless => OrderType == "Takeaway" || OrderType == "Online";

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

                if (SetProperty(ref _selectedQty, value))
                {
                    if (SelectedCartRow != null)
                    {
                        _cartService.SetQuantity(TableNumber, SelectedCartRow.ItemId, value);
                        UpdateCart();
                    }
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

        public BillingViewModel(IItemService itemService, ICartService cartService,
                                IOrderService orderService, ISettingService settingService,
                                ICategoryService categoryService, INotificationService notificationService,
                                ICashService cashService, ITableService tableService,
                                IDialogService? dialogService = null)
        {
            _itemService = itemService;
            _cartService = cartService;
            _orderService = orderService;
            _settingService = settingService;
            _categoryService = categoryService;
            _notificationService = notificationService;
            _cashService = cashService;
            _tableService = tableService;
            _dialogService = dialogService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(itemService);
                App.RegisterTestService(cartService);
                App.RegisterTestService(orderService);
                App.RegisterTestService(settingService);
                App.RegisterTestService(categoryService);
                App.RegisterTestService(notificationService);
                App.RegisterTestService(cashService);
                App.RegisterTestService(tableService);
                if (dialogService != null) App.RegisterTestService(dialogService);
            }

            LoadHeldOrders();
        }

        [ObservableProperty]
        private ObservableCollection<HeldOrder> _heldOrders = new();

        [ObservableProperty]
        private bool _isHeldOrdersPopupOpen;

        [ObservableProperty]
        private bool _isEditMode;
    }
}
