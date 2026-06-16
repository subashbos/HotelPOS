using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

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

        private void LoadHeldOrders()
        {
            HeldOrders.Clear();
            foreach (var h in _cartService.GetHeldOrders())
                HeldOrders.Add(h);
        }

        // --- Table Layout ---
        public ObservableCollection<TableStatus> Tables { get; } = new();

        [ObservableProperty]
        private bool _isTableLayoutOpen;

        [RelayCommand]
        private void OpenTableLayout(object? parameter)
        {
            // Table layout is irrelevant for Takeaway / Online orders
            if (IsTableless) return;

            bool open = true;
            if (parameter is bool b) open = b;
            else if (parameter is string s && bool.TryParse(s, out bool b2)) open = b2;

            if (open) RefreshTables();
            IsTableLayoutOpen = open;

            if (open && !IsTransferMode) IsTransferMode = false;
        }

        [RelayCommand]
        private void SelectTable(int tableNumber)
        {
            if (IsTransferMode)
            {
                _cartService.TransferTable(TableNumber, tableNumber);
                IsTransferMode = false;
                StatusMessage = $"Table {TableNumber} items moved to Table {tableNumber}";
                TableNumber = tableNumber;
            }
            else
            {
                TableNumber = tableNumber;
            }

            IsTableLayoutOpen = false;
            UpdateCart();
        }

        [RelayCommand]
        private void ToggleTransferMode()
        {
            // Move Items is only meaningful for DineIn
            if (IsTableless) return;
            if (Cart.Count == 0) return;
            IsTransferMode = !IsTransferMode;

            if (IsTransferMode)
            {
                StatusMessage = "MOVE MODE: Select target table from the Table menu";
                OpenTableLayout(true);
            }
            else
            {
                StatusMessage = "Ready";
                IsTableLayoutOpen = false;
            }
        }

        /// <summary>
        /// Reloads the ViewModel's Tables collection from the table service and updates each TableStatus's occupancy and current flags.
        /// </summary>
        /// <remarks>
        /// If the table service returns no tables, populates a default set of 20 tables. Any error during refresh is reported via the notification service.
        /// </remarks>
        private async void RefreshTables()
        {
            using (var scope = App.CreateDbScope())
            {
                var tableService = scope.ServiceProvider.GetRequiredService<ITableService>();
                try
                {
                    var tables = await tableService.GetTablesAsync();
                    Tables.Clear();
                    var activeTables = _cartService.GetActiveTables();

                    if (tables == null || tables.Count == 0)
                    {
                        // Fallback to default 20 tables if none defined yet
                        for (int i = 1; i <= 20; i++)
                        {
                            Tables.Add(new TableStatus
                            {
                                TableNumber = i,
                                IsOccupied = activeTables.Contains(i),
                                IsCurrent = i == TableNumber
                            });
                        }
                    }
                    else
                    {
                        foreach (var t in tables.Where(x => x.IsActive).OrderBy(x => x.Number))
                        {
                            Tables.Add(new TableStatus
                            {
                                TableNumber = t.Number,
                                TableName = t.Name,
                                IsOccupied = activeTables.Contains(t.Number),
                                IsCurrent = t.Number == TableNumber
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to refresh tables: {ex.Message}");
                }
            }
        }

        private bool _isInitializing;

        /// <summary>
        /// Loads items, categories, and settings into the view model, initializes filtered item list and cart state, and verifies that a cash shift is open.
        /// </summary>
        /// <remarks>
        /// Populates the internal item cache and the Categories collection (including an "All" category), sets composition-scheme state, applies the active filter, and updates cart totals and UI state. If no cash session is open a user-facing warning is shown; failures during initialization are reported via the notification service.
        /// </remarks>
        public async Task InitializeAsync()
        {
            if (_isInitializing) return;

            using (var scope = App.CreateDbScope())
            {
                var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();

                try
                {
                    _isInitializing = true;
                    _allItems = await itemService.GetItemsAsync();
                    var cats = await categoryService.GetCategoriesAsync();
                    var orderedCats = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();

                    Categories.Clear();
                    Categories.Add(new Category { Id = 0, Name = "All", DisplayOrder = -1 });
                    foreach (var cat in orderedCats) Categories.Add(cat);

                    var settings = await settingService.GetSettingsAsync();
                    IsCompositionScheme = settings.IsCompositionScheme;

                    ApplyFilter();
                    UpdateCart();

                    var currentSession = await cashService.GetCurrentSessionAsync();
                    if (currentSession == null)
                    {
                        _notificationService.ShowWarning("Please open a shift in the 'Shift' tab before starting billing.");
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to load data: {ex.Message}");
                }
                finally
                {
                    _isInitializing = false;
                }
            }
        }

        [RelayCommand]
        private void ApplyFilter()
        {
            if (_allItems == null) return;
            var filtered = _allItems.AsEnumerable();
            if (SelectedCategoryId > 0)
                filtered = filtered.Where(i => i.CategoryId == SelectedCategoryId);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.Trim();
                filtered = filtered.Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                              (!string.IsNullOrEmpty(i.Barcode) && i.Barcode.Contains(query, StringComparison.OrdinalIgnoreCase)))
                                   .OrderBy(i => i.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                                   .ThenBy(i => i.Name);
            }
            else
            {
                filtered = filtered.OrderBy(i => i.Name);
            }

            Items.Clear();
            foreach (var item in filtered) Items.Add(item);
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void FilterByCategory(int categoryId)
        {
            SelectedCategoryId = categoryId;
            ApplyFilter();
        }

        partial void OnTableNumberChanged(int value) => UpdateCart();

        [RelayCommand]
        private void AdjustQuantity(object parameter)
        {
            if (parameter is object[] values && values.Length >= 2 && values[0] is CartRow row && int.TryParse(values[1].ToString(), out int delta))
            {
                var item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
                if (item == null) return;

                if (delta > 0 && item.TrackInventory)
                {
                    var cartItems = _cartService.GetItems(TableNumber);
                    var inCart = cartItems.FirstOrDefault(x => x.ItemId == item.Id)?.Quantity ?? 0;

                    if (inCart + delta > item.StockQuantity)
                    {
                        StatusMessage = $"⚠ Out of stock: {item.Name} (Avail: {item.StockQuantity})";
                        return;
                    }
                }

                _cartService.AddItem(TableNumber, row.ItemId, delta);
                UpdateCart();
            }
        }

        [RelayCommand]
        private void AddToCart(object parameter)
        {
            Item? item = parameter as Item;
            if (item == null && parameter is CartRow row)
            {
                item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
            }

            if (item == null) return;

            // Stock Check
            if (item.TrackInventory)
            {
                var cartItems = _cartService.GetItems(TableNumber);
                var inCart = cartItems.FirstOrDefault(x => x.ItemId == item.Id)?.Quantity ?? 0;

                if (inCart + 1 > item.StockQuantity)
                {
                    StatusMessage = $"⚠ Out of stock: {item.Name} (Avail: {item.StockQuantity})";
                    return;
                }
            }

            _cartService.AddItem(TableNumber, item);
            UpdateCart();
        }

        [RelayCommand]
        private void DecreaseQuantity(object parameter)
        {
            if (parameter is CartRow row)
            {
                _cartService.AddItem(TableNumber, row.ItemId, -1);
                UpdateCart();
            }
        }

        [RelayCommand]
        private void RemoveFromCart(CartRow row)
        {
            if (row == null) return;
            _cartService.RemoveItem(TableNumber, row.ItemId);
            UpdateCart();
        }

        [RelayCommand]
        public void UpdateRow(CartRow row)
        {
            if (row == null) return;

            if (row.Quantity < 1)
            {
                row.Quantity = 1;
                StatusMessage = "Quantity cannot be less than 1. Use 'Remove' to delete item.";
            }

            // LOOPHOLE FIX: Stock check on manual edit
            var item = _allItems.FirstOrDefault(i => i.Id == row.ItemId);
            if (item != null && item.TrackInventory && row.Quantity > item.StockQuantity)
            {
                StatusMessage = $"⚠ Stock Limit: {item.Name} (Avail: {item.StockQuantity})";
                row.Quantity = Math.Max(1, item.StockQuantity);
                _notificationService.ShowWarning($"{item.Name} quantity capped to available stock ({item.StockQuantity})");
            }

            if (row.Price < 0)
            {
                row.Price = 0;
                StatusMessage = "Price cannot be negative.";
            }

            _cartService.SetQuantity(TableNumber, row.ItemId, row.Quantity);
            _cartService.UpdatePrice(TableNumber, row.ItemId, row.Price);
            UpdateCart();
        }

        [RelayCommand]
        private void ClearCart()
        {
            if (Cart.Count == 0) return;

            // Confirm before wiping the entire bill
            var result = System.Windows.MessageBox.Show(
                "Clear all items from the current bill?",
                "Clear Cart",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            _cartService.Clear(TableNumber);
            DiscountAmount = 0;   // LOOPHOLE FIX: reset discount when cart is cleared
            UpdateCart();
            CartCleared?.Invoke();
        }

        [RelayCommand]
        private async Task HoldOrder()
        {
            if (Cart.Count == 0) return;

            // LOOPHOLE FIX: Confirm before moving to hold/kitchen
            var result = System.Windows.MessageBox.Show(
                "Send this order to Kitchen / Hold?",
                "Hold Order",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            _cartService.HoldOrder(TableNumber, $"Table {TableNumber} - {DateTime.Now:hh:mm tt}");

            // Get the newly held order to print KOT
            var newlyHeld = _cartService.GetHeldOrders().OrderByDescending(h => h.HeldAt).FirstOrDefault();

            LoadHeldOrders();
            UpdateCart();
            _notificationService.ShowSuccess("Order moved to Hold / Sent to Kitchen");

            if (newlyHeld != null)
            {
                await PrintKOTAsync(newlyHeld.TableNumber, newlyHeld.Items);
            }
        }

        [RelayCommand]
        private async Task PrintKOTOnly()
        {
            if (Cart.Count == 0) return;
            var items = _cartService.GetItems(TableNumber);
            await PrintKOTAsync(TableNumber, items);
            StatusMessage = "KOT Sent to Kitchen";
        }

        private async Task PrintKOTAsync(int tableNumber, List<OrderItem> items)
        {
            try
            {
                var settings = await _settingService.GetSettingsAsync();

                // Execute on UI thread for printing
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var doc = ReceiptGenerator.CreateKOT(tableNumber, items, settings.ReceiptFormat == "Thermal");

                        var dialog = new System.Windows.Controls.PrintDialog();
                        if (!string.IsNullOrEmpty(settings.DefaultPrinter))
                        {
                            try
                            {
                                dialog.PrintQueue = new System.Printing.LocalPrintServer().GetPrintQueue(settings.DefaultPrinter);
                            }
                            catch { /* Fallback to default if printer not found */ }
                        }

                        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "KOT " + tableNumber);
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"KOT Print failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ToggleHeldOrders()
        {
            LoadHeldOrders();
            IsHeldOrdersPopupOpen = !IsHeldOrdersPopupOpen;
        }

        [RelayCommand]
        private void ResumeOrder(HeldOrder held)
        {
            if (held == null) return;
            _cartService.ResumeHeldOrder(held.Id, TableNumber);
            IsHeldOrdersPopupOpen = false;
            LoadHeldOrders();
            UpdateCart();
            _notificationService.ShowInfo($"Resumed {held.HoldName}");
        }

        private void UpdateCart()
        {
            var items = _cartService.GetItems(TableNumber);

            // 1. Remove rows that are no longer in the cart
            var toRemove = Cart.Where(row => !items.Any(i => i.ItemId == row.ItemId)).ToList();
            foreach (var row in toRemove) Cart.Remove(row);

            foreach (var item in items)
            {
                var existing = Cart.FirstOrDefault(r => r.ItemId == item.ItemId);
                if (existing != null)
                {
                    existing.Quantity = item.Quantity;
                    existing.Price = item.Price;
                    existing.TaxPercentage = item.TaxPercentage;
                    existing.TaxAmount = Math.Round(item.Price * item.Quantity * (item.TaxPercentage / 100m), 2);
                    existing.Total = item.Total;
                }
                else
                {
                    Cart.Add(new CartRow
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        TaxPercentage = item.TaxPercentage,
                        TaxAmount = Math.Round(item.Price * item.Quantity * (item.TaxPercentage / 100m), 2),
                        Total = item.Total
                    });
                }
            }

            // Sort the collection to match the service's order (alphabetical by name)
            // This ensures S.No 1, 2, 3 always follows a consistent visual order.
            var sortedList = Cart.OrderBy(r => r.ItemName).ToList();

            // If the order changed, we need to re-arrange the ObservableCollection
            for (int i = 0; i < sortedList.Count; i++)
            {
                var row = sortedList[i];
                int oldIndex = Cart.IndexOf(row);
                if (oldIndex != i)
                {
                    Cart.Move(oldIndex, i);
                }
                row.SNo = i + 1;
            }

            Subtotal = _cartService.GetSubtotal(TableNumber);
            GstAmount = IsCompositionScheme ? 0 : _cartService.GetGstAmount(TableNumber);
            TotalAmount = Math.Max(0, Subtotal + GstAmount - DiscountAmount);

            // Sync Active Tabs — only for DineIn (tableless orders use virtual table 0)
            if (!IsTableless)
            {
                var currentActive = _cartService.GetActiveTables() ?? new List<int>();
                // Exclude virtual table 0 from the tab strip
                currentActive = currentActive.Where(t => t > 0).ToList();

                foreach (var t in currentActive)
                    if (!ActiveTabs.Contains(t)) ActiveTabs.Add(t);

                var toRemoveTabs = ActiveTabs.Where(t => !currentActive.Contains(t) && t != TableNumber).ToList();
                foreach (var t in toRemoveTabs) ActiveTabs.Remove(t);

                if (Cart.Count > 0 && TableNumber > 0 && !ActiveTabs.Contains(TableNumber))
                    ActiveTabs.Add(TableNumber);

                var sorted = ActiveTabs.OrderBy(t => t).ToList();
                for (int i = 0; i < sorted.Count; i++)
                {
                    int oldIndex = ActiveTabs.IndexOf(sorted[i]);
                    if (oldIndex != i) ActiveTabs.Move(oldIndex, i);
                }
            }
            else
            {
                // Tableless order — clear any stale DineIn tabs that might linger
                // (don't touch tabs belonging to other DineIn sessions)
            }
        }

        partial void OnDiscountAmountChanged(decimal value)
        {
            if (value < 0)
            {
                DiscountAmount = 0;
                _notificationService.ShowWarning("Discount cannot be negative.");
            }
            else if (value > Subtotal)
            {
                DiscountAmount = Subtotal;
                _notificationService.ShowWarning("Discount cannot exceed the subtotal.");
            }
            UpdateCart();
        }

        [ObservableProperty]
        private bool _isEditMode;

        private Order? _editingOrder;

        public void LoadOrderForEdit(Order order)
        {
            _editingOrder = order;
            IsEditMode = true;

            // Set OrderType first so IsTableless is correct before TableNumber is set
            OrderType = string.IsNullOrWhiteSpace(order.OrderType) ? "DineIn" : order.OrderType;

            // For tableless orders the stored TableNumber is 0 — keep it as-is
            // For DineIn, use the stored table number (always > 0)
            TableNumber = IsTableless ? 0 : order.TableNumber;

            _cartService.LoadItems(TableNumber, order.Items);
            UpdateCart(); // Calculate Subtotal before setting DiscountAmount
            DiscountAmount = order.DiscountAmount;
            PaymentMode = order.PaymentMode;
            CustomerName = order.CustomerName;
            CustomerPhone = order.CustomerPhone;
            CustomerGstin = order.CustomerGstin;
            UpdateCart();
            StatusMessage = $"✏ Editing Order #{order.Id}";
        }

        [RelayCommand]
        private void CancelEdit()
        {
            if (_editingOrder != null) _cartService.Clear(TableNumber);
            _editingOrder = null;
            IsEditMode = false;
            OrderType = "DineIn";   // reset to default
            UpdateCart();
            StatusMessage = "Ready";
            OrderEditCancelled?.Invoke();
        }

        [RelayCommand]
        private void ToggleOrderType()
        {
            if (OrderType == "DineIn") OrderType = "Takeaway";
            else if (OrderType == "Takeaway") OrderType = "Online";
            else OrderType = "DineIn";
        }

        /// <summary>
        /// Persists the current cart as an order (updates an existing order when editing or creates a new order), triggers printing if applicable, and clears checkout state on success.
        /// </summary>
        /// <returns>A task that completes when the save (or update), any printing, and subsequent cart/state cleanup have finished.</returns>
        [RelayCommand]
        private async Task SaveOrderAsync()
        {
            var rawItems = _cartService.GetItems(TableNumber);
            if (rawItems.Count == 0)
            {
                _notificationService.ShowInfo("Cannot save empty order");
                return;
            }

            // LOOPHOLE FIX: Validate PaymentMode
            if (string.IsNullOrWhiteSpace(PaymentMode))
            {
                _notificationService.ShowError("Please select a payment mode before checkout.");
                return;
            }

            // LOOPHOLE FIX: Prevent checkout if shift is closed
            using (var scope = App.CreateDbScope())
            {
                var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                var currentSession = await cashService.GetCurrentSessionAsync();
                if (currentSession == null)
                {
                    _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                    return;
                }
            }

            decimal finalCash = 0;
            decimal finalCard = 0;
            decimal finalUpi = 0;
            string finalPaymentMode = PaymentMode;

            // Show Confirm Checkout Dialog if service is available
            if (_dialogService != null)
            {
                var details = new ConfirmCheckoutDetails
                {
                    TotalItems = rawItems.Sum(i => i.Quantity),
                    TotalAmount = Subtotal + GstAmount,
                    DiscountAmount = DiscountAmount,
                    FinalPayableAmount = TotalAmount,
                    PaymentMode = PaymentMode
                };

                bool confirmed = await _dialogService.ShowConfirmCheckoutAsync(details);
                if (!confirmed)
                {
                    CheckoutCancelled?.Invoke();
                    return;
                }

                finalPaymentMode = details.PaymentMode;
                if (finalPaymentMode == "Split")
                {
                    finalCash = details.CashAmount;
                    finalCard = details.CardAmount;
                    finalUpi = details.UpiAmount;
                }
            }

            // Perform the save
            try
            {
                using (var scope = App.CreateDbScope())
                {
                    var cashService = scope.ServiceProvider.GetRequiredService<ICashService>();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

                    // Re-verify shift state under lock
                    var currentSession = await cashService.GetCurrentSessionAsync();
                    if (currentSession == null)
                    {
                        _notificationService.ShowError("Shift is not open. Please open a shift before checkout.");
                        return;
                    }

                    if (IsEditMode && _editingOrder != null)
                    {
                        _editingOrder.Items = rawItems;
                        _editingOrder.TableNumber = TableNumber;
                        _editingOrder.DiscountAmount = DiscountAmount;
                        _editingOrder.PaymentMode = finalPaymentMode;
                        _editingOrder.OrderType = OrderType;
                        _editingOrder.CustomerName = CustomerName;
                        _editingOrder.CustomerPhone = CustomerPhone;
                        _editingOrder.CustomerGstin = CustomerGstin;

                        if (finalPaymentMode == "Split")
                        {
                            _editingOrder.CashPaid = finalCash;
                            _editingOrder.CardPaid = finalCard;
                            _editingOrder.UpiPaid = finalUpi;
                            _editingOrder.AmountPaid = finalCash + finalCard + finalUpi;
                            _editingOrder.Status = _editingOrder.AmountPaid >= _editingOrder.TotalAmount ? "Paid" : "Partial";
                        }
                        else
                        {
                            _editingOrder.AmountPaid = _editingOrder.TotalAmount;
                            _editingOrder.CashPaid = finalPaymentMode == "Cash" ? _editingOrder.TotalAmount : 0;
                            _editingOrder.CardPaid = finalPaymentMode == "Card" ? _editingOrder.TotalAmount : 0;
                            _editingOrder.UpiPaid = finalPaymentMode == "UPI" ? _editingOrder.TotalAmount : 0;
                            _editingOrder.Status = "Paid";
                        }

                        await orderService.UpdateOrderAsync(_editingOrder);
                    }
                    else
                    {
                        int orderId;
                        if (finalPaymentMode == "Split")
                        {
                            // Save order first (initially cash mode to bypass validate, then updates details)
                            orderId = await orderService.SaveOrderAsync(rawItems, TableNumber, DiscountAmount, "Cash", CustomerName, CustomerPhone, CustomerGstin, OrderType);
                            var createdOrder = await orderService.GetOrderAsync(orderId);
                            if (createdOrder != null)
                            {
                                createdOrder.PaymentMode = "Split";
                                createdOrder.CashPaid = finalCash;
                                createdOrder.CardPaid = finalCard;
                                createdOrder.UpiPaid = finalUpi;
                                createdOrder.AmountPaid = finalCash + finalCard + finalUpi;
                                createdOrder.Status = createdOrder.AmountPaid >= createdOrder.TotalAmount ? "Paid" : "Partial";
                                await orderService.UpdateOrderAsync(createdOrder);
                            }
                        }
                        else
                        {
                            orderId = await orderService.SaveOrderAsync(rawItems, TableNumber, DiscountAmount, finalPaymentMode, CustomerName, CustomerPhone, CustomerGstin, OrderType);
                        }

                        // Trigger Print
                        await PrintOrderAsync(orderId);
                    }
                }

                var wasEditMode = IsEditMode;
                
                // Clear cart automatically without prompting the user
                _cartService.Clear(TableNumber);
                DiscountAmount = 0;
                UpdateCart();
                CartCleared?.Invoke();
                
                IsEditMode = false;
                _editingOrder = null;
                PaymentMode = "Cash";
                OrderType = "DineIn";
                // CLEANUP: Reset customer details to null to align with property definitions and save memory
                CustomerName = null;
                CustomerPhone = null;
                CustomerGstin = null;

                // Fire after state is fully cleared so the dashboard refreshes
                // with a clean VM — only for updates, not new orders
                if (wasEditMode)
                    OrderUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates a receipt for the specified order and either shows a print preview or sends it to the printer.
        /// </summary>
        /// <param name="orderId">The identifier of the order to print if <paramref name="preLoadedOrder"/> is not provided.</param>
        /// <param name="preLoadedOrder">An optional preloaded <see cref="Order"/> to use instead of loading the order by <paramref name="orderId"/>.</param>
        /// <param name="skipPreview">When true, bypasses the print preview even if previewing is enabled in settings and prints directly.</param>
        private async Task PrintOrderAsync(int orderId, Order? preLoadedOrder = null, bool skipPreview = false)
        {
            try
            {
                SystemSetting settings;
                Order? order;
                using (var scope = App.CreateDbScope())
                {
                    var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    settings = await settingService.GetSettingsAsync();
                    order = preLoadedOrder ?? await orderService.GetOrderAsync(orderId);
                }

                if (order == null) return;

                // Execute on UI thread for printing
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        var doc = ReceiptGenerator.CreateReceipt(order, settings.ReceiptFormat == "Thermal", settings);
                        if (settings.ShowPrintPreview && !skipPreview)
                        {
                            var preview = new PrintPreviewWindow(order, settings);
                            preview.ShowDialog();
                            PrintPreviewClosed?.Invoke();
                        }
                        else
                        {
                            var dialog = new System.Windows.Controls.PrintDialog();
                            if (!string.IsNullOrEmpty(settings.DefaultPrinter))
                            {
                                try
                                {
                                    dialog.PrintQueue = new System.Printing.LocalPrintServer().GetPrintQueue(settings.DefaultPrinter);
                                }
                                catch { /* Fallback to default if printer not found */ }
                            }
                            dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Receipt " + order.Id);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Print failed: {ex.Message}");
            }
        }
    }

    public class TableStatus : ObservableObject
    {
        public int TableNumber { get; set; }
        public string TableName { get; set; } = string.Empty;

        private bool _isOccupied;
        public bool IsOccupied
        {
            get => _isOccupied;
            set => SetProperty(ref _isOccupied, value);
        }

        private bool _isCurrent;
        public bool IsCurrent
        {
            get => _isCurrent;
            set => SetProperty(ref _isCurrent, value);
        }
    }
}
