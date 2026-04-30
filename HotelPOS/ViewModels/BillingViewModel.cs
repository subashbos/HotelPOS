using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
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

        [ObservableProperty]
        private string _statusMessage = "Ready";
        
        [ObservableProperty]
        private decimal _discountAmount;

        [ObservableProperty]
        private string _paymentMode = "Cash";

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

        private List<Item> _allItems = new();

        public BillingViewModel(IItemService itemService, ICartService cartService, 
                                IOrderService orderService, ISettingService settingService,
                                ICategoryService categoryService, INotificationService notificationService,
                                ICashService cashService)
        {
            _itemService = itemService;
            _cartService = cartService;
            _orderService = orderService;
            _settingService = settingService;
            _categoryService = categoryService;
            _notificationService = notificationService;
            _cashService = cashService;
            
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
        private void OpenTableLayout()
        {
            RefreshTables();
            IsTableLayoutOpen = true;
        }

        [RelayCommand]
        private void SelectTable(int tableNumber)
        {
            TableNumber = tableNumber;
            IsTableLayoutOpen = false;
            UpdateCart();
        }

        private void RefreshTables()
        {
            Tables.Clear();
            var activeTables = _cartService.GetActiveTables();

            // Assume restaurant has 20 tables for now
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

        public async Task InitializeAsync()
        {
            try
            {
                _allItems = await _itemService.GetItemsAsync();
                var cats = await _categoryService.GetCategoriesAsync();

                Categories.Clear();
                Categories.Add(new Category { Id = 0, Name = "All" });
                foreach (var cat in cats) Categories.Add(cat);

                ApplyFilter();
                UpdateCart();

                var currentSession = await _cashService.GetCurrentSessionAsync();
                if (currentSession == null)
                {
                    _notificationService.ShowWarning("Please open a shift in the 'Shift' tab before starting billing.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load data: {ex.Message}");
            }
        }

        [RelayCommand]
        private void ApplyFilter()
        {
            var filtered = _allItems.AsEnumerable();
            if (SelectedCategoryId > 0)
                filtered = filtered.Where(i => i.CategoryId == SelectedCategoryId);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                                      (!string.IsNullOrEmpty(i.Barcode) && i.Barcode.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

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
            _notificationService.ShowInfo($"Added {item.Name}");
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
            _notificationService.ShowInfo($"Removed {row.ItemName}");
        }

        [RelayCommand]
        private void ClearCart()
        {
            _cartService.Clear(TableNumber);
            UpdateCart();
            _notificationService.ShowInfo("Cart cleared");
        }

        [RelayCommand]
        private async Task HoldOrder()
        {
            if (Cart.Count == 0) return;
            
            _cartService.HoldOrder(TableNumber, $"Table {TableNumber} - {DateTime.Now:hh:mm tt}");
            
            // Get the newly held order to print KOT
            var newlyHeld = _cartService.GetHeldOrders().OrderByDescending(h => h.HeldAt).FirstOrDefault();
            
            LoadHeldOrders();
            UpdateCart();
            _notificationService.ShowSuccess("Order moved to Hold / Sent to Kitchen");

            if (newlyHeld != null)
            {
                await PrintKOTAsync(newlyHeld);
            }
        }

        private async Task PrintKOTAsync(HeldOrder heldOrder)
        {
            try
            {
                var settings = await _settingService.GetSettingsAsync();

                // Execute on UI thread for printing
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var doc = ReceiptGenerator.CreateKOT(heldOrder.TableNumber, heldOrder.Items, settings.ReceiptFormat == "Thermal");
                    
                    var dialog = new System.Windows.Controls.PrintDialog();
                    if (!string.IsNullOrEmpty(settings.DefaultPrinter))
                    {
                        try
                        {
                            dialog.PrintQueue = new System.Printing.PrintServer().GetPrintQueue(settings.DefaultPrinter);
                        }
                        catch { /* Fallback to default if printer not found */ }
                    }
                    
                    // We don't preview KOTs, we just send them straight to the printer.
                    // If no default printer is set or it's invalid, it might pop up the dialog if we call ShowDialog.
                    // Let's just try to print document directly. If they haven't configured a printer, it goes to default.
                    dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "KOT " + heldOrder.TableNumber);
                });
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
            Cart.Clear();
            var items = _cartService.GetItems(TableNumber);
            int sno = 1;
            foreach (var item in items)
            {
                Cart.Add(new CartRow
                {
                    SNo = sno++,
                    ItemId = item.ItemId,
                    ItemName = item.ItemName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    TaxPercentage = item.TaxPercentage,
                    TaxAmount = Math.Round(item.Price * item.Quantity * (item.TaxPercentage / 100m), 2),
                    Total = item.Total
                });
            }

            Subtotal = _cartService.GetSubtotal(TableNumber);
            GstAmount = _cartService.GetGstAmount(TableNumber);
            TotalAmount = Math.Max(0, Subtotal + GstAmount - DiscountAmount);
        }

        partial void OnDiscountAmountChanged(decimal value) => UpdateCart();

        [ObservableProperty]
        private bool _isEditMode;

        private Order? _editingOrder;

        public void LoadOrderForEdit(Order order)
        {
            _editingOrder = order;
            IsEditMode = true;
            TableNumber = order.TableNumber;
            _cartService.LoadItems(order.TableNumber, order.Items);
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
            if (_editingOrder != null) _cartService.Clear(_editingOrder.TableNumber);
            _editingOrder = null;
            IsEditMode = false;
            UpdateCart();
            StatusMessage = "Ready";
        }

        [RelayCommand]
        private async Task SaveOrderAsync()
        {
            var rawItems = _cartService.GetItems(TableNumber);
            if (rawItems.Count == 0)
            {
                _notificationService.ShowInfo("Cannot save empty order");
                return;
            }

            try
            {
                if (IsEditMode && _editingOrder != null)
                {
                    _editingOrder.Items = rawItems;
                    _editingOrder.TableNumber = TableNumber;
                    _editingOrder.DiscountAmount = DiscountAmount;
                    _editingOrder.PaymentMode = PaymentMode;
                    _editingOrder.CustomerName = CustomerName;
                    _editingOrder.CustomerPhone = CustomerPhone;
                    _editingOrder.CustomerGstin = CustomerGstin;

                    await _orderService.UpdateOrderAsync(_editingOrder);
                    _notificationService.ShowSuccess($"Order #{_editingOrder.Id} updated successfully");
                    await PrintOrderAsync(_editingOrder.Id);
                }
                else
                {
                    int orderId = await _orderService.SaveOrderAsync(rawItems, TableNumber, DiscountAmount, PaymentMode, CustomerName, CustomerPhone, CustomerGstin);
                    _notificationService.ShowSuccess($"Order #{orderId} saved successfully");

                    // Trigger Print
                    await PrintOrderAsync(orderId);
                }
                
                ClearCart();
                IsEditMode = false;
                _editingOrder = null;
                DiscountAmount = 0;
                PaymentMode = "Cash";
                CustomerName = string.Empty;
                CustomerPhone = string.Empty;
                CustomerGstin = string.Empty;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Save failed: {ex.Message}");
            }
        }

        private async Task PrintOrderAsync(int orderId)
        {
            try
            {
                var settings = await _settingService.GetSettingsAsync();
                var orders = await _orderService.GetAllOrdersWithItemsAsync();
                var order = orders.FirstOrDefault(o => o.Id == orderId);

                if (order == null) return;

                // Execute on UI thread for printing
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var doc = ReceiptGenerator.CreateReceipt(order, settings.ReceiptFormat == "Thermal", settings);
                    if (settings.ShowPrintPreview)
                    {
                        var preview = new PrintPreviewWindow(order, settings);
                        preview.ShowDialog();
                    }
                    else
                    {
                        var dialog = new System.Windows.Controls.PrintDialog();
                        if (!string.IsNullOrEmpty(settings.DefaultPrinter))
                        {
                            try
                            {
                                dialog.PrintQueue = new System.Printing.PrintServer().GetPrintQueue(settings.DefaultPrinter);
                            }
                            catch { /* Fallback to default if printer not found */ }
                        }
                        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Receipt " + order.Id);
                    }
                });
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
