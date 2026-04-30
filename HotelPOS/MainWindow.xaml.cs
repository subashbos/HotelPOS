using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HotelPOS
{
    public partial class MainWindow : Window
    {


        private readonly IItemService _itemService;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private List<Item> _allItems = new();
        private List<Item> _visibleItems = new();
        private List<MainCartRow> _cartRows = new();

        public MainWindow(
            IItemService itemService,
            ICartService cartService,
            IOrderService orderService,
            ISettingService settingService)
        {
            InitializeComponent();

            _itemService = itemService;
            _cartService = cartService;
            _orderService = orderService;
            _settingService = settingService;

            TableSelector.SelectedIndex = 0;

            LoadItems();
            RefreshCart();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // If the user is an Admin, they likely came from the Dashboard.
            // If they are a Cashier, they started here, so closing means logout.
            if (!AppSession.IsAdmin)
            {
                var result = MessageBox.Show("Log out and return to Login screen?", "Shifting Shifts", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    AppSession.Logout();
                    var loginWindow = ((App)System.Windows.Application.Current).ServiceProvider.GetRequiredService<LoginWindow>();
                    loginWindow.Show();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            // Admins just close this window and return to their owner (Dashboard)
        }

        private async void LoadItems()
        {
            // async void is required for event-like startup calls — guard with try/catch
            try
            {
                _allItems = await _itemService.GetItemsAsync();
                ApplyItemFilter();

                ShowStatus(_allItems.Count == 0
                    ? "No menu items found. Add items to start billing."
                    : $"{_allItems.Count} menu items loaded for fast billing.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Unable to load menu items: {ex.Message}");
            }
        }

        private int SelectedTableNumber =>
            TableSelector.SelectedItem is ComboBoxItem selectedItem &&
            int.TryParse(selectedItem.Tag?.ToString(), out var tableNumber)
                ? tableNumber
                : 1;

        private void ApplyItemFilter()
        {
            var search = SearchBox.Text?.Trim() ?? string.Empty;

            _visibleItems = string.IsNullOrWhiteSpace(search)
                ? _allItems.OrderBy(x => x.Name).ToList()
                : _allItems
                    .Where(x => x.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Name)
                    .ToList();

            ItemList.ItemsSource = null;
            ItemList.ItemsSource = _visibleItems;
        }

        private void AddSelectedItemToCart()
        {
            if (ItemList.SelectedItem is not Item item)
            {
                ShowStatus("Select a menu item to add it to the bill.");
                return;
            }

            _cartService.AddItem(SelectedTableNumber, item);
            ItemList.SelectedItem = null;

            ShowStatus($"{item.Name} added to Table {SelectedTableNumber}.");
            RefreshCart();
        }

        private void RefreshCart()
        {
            var tableNumber = SelectedTableNumber;
            var subtotal = _cartService.GetSubtotal(tableNumber);
            var gst = _cartService.GetGstAmount(tableNumber);
            var grandTotal = _cartService.GetGrandTotal(tableNumber);
            var sourceItems = _cartService.GetItems(tableNumber);

            _cartRows = sourceItems
                .Select(i => new MainCartRow
                {
                    ItemId = i.ItemId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    TaxPercentage = i.TaxPercentage,
                    TaxAmount = Math.Round(i.Price * i.Quantity * (i.TaxPercentage / 100m), 2),
                    Total = i.Total
                })
                .ToList();

            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = _cartRows;

            TotalText.Text = $"Rs. {subtotal:N2}";
            GstText.Text = $"Rs. {gst:N2}";
            FinalText.Text = $"Rs. {grandTotal:N2}";
        }

        private void ShowStatus(string message)
        {
            StatusText.Text = message;
        }

        private async Task CheckoutCurrentTableAsync()
        {
            var tableNumber = SelectedTableNumber;
            var items = _cartService.GetItems(tableNumber);

            if (items.Count == 0)
            {
                ShowStatus($"Table {tableNumber} has no items to bill.");
                return;
            }

            try
            {
                int orderId = await _orderService.SaveOrderAsync(items, tableNumber);

                var subtotal = _cartService.GetSubtotal(tableNumber);
                var gst = _cartService.GetGstAmount(tableNumber);
                var grandTotal = _cartService.GetGrandTotal(tableNumber);

                // Construct an order model for the print preview
                var printOrder = new Order
                {
                    Id = orderId,
                    TableNumber = tableNumber,
                    CreatedAt = DateTime.UtcNow,
                    Subtotal = subtotal,
                    GstAmount = gst,
                    TotalAmount = grandTotal,
                    Items = items.Select(i => new OrderItem
                    {
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        TaxPercentage = i.TaxPercentage,
                        Total = i.Total
                    }).ToList()
                };

                _cartService.Clear(tableNumber);
                RefreshCart();
                ShowStatus($"Checkout completed for Table {tableNumber}. Order #{orderId}");

                // Show print preview blocking dialog
                var settings = await _settingService.GetSettingsAsync();
                var previewWindow = new PrintPreviewWindow(printOrder, settings) { Owner = this };
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowStatus($"Checkout failed: {ex.Message}");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyItemFilter();
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedItemToCart();
        }

        private void ItemList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddSelectedItemToCart();
        }

        private void TableSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            RefreshCart();
            ShowStatus($"Switched to Table {SelectedTableNumber}.");
        }

        private void AdjustQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (CartGrid.SelectedItem is not MainCartRow selectedItem)
            {
                ShowStatus("Select a cart item to change quantity.");
                return;
            }

            if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out var change))
            {
                return;
            }

            _cartService.UpdateQuantity(SelectedTableNumber, selectedItem.ItemId, change);
            RefreshCart();
            ShowStatus($"{selectedItem.ItemName} quantity updated for Table {SelectedTableNumber}.");
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelectedCartItem();
        }

        private void ClearCart_Click(object sender, RoutedEventArgs e)
        {
            _cartService.Clear(SelectedTableNumber);
            RefreshCart();
            ShowStatus($"Cleared billing cart for Table {SelectedTableNumber}.");
        }

        private async void Checkout_Click(object sender, RoutedEventArgs e)
        {
            // async void event handler — must catch all exceptions to prevent app crash
            try
            {
                await CheckoutCurrentTableAsync();
            }
            catch (Exception ex)
            {
                ShowStatus($"Unexpected checkout error: {ex.Message}");
            }
        }

        private void RemoveSelectedCartItem()
        {
            if (CartGrid.SelectedItem is not MainCartRow selectedItem)
            {
                ShowStatus("Select a cart item to remove.");
                return;
            }

            _cartService.RemoveItem(SelectedTableNumber, selectedItem.ItemId);
            RefreshCart();
            ShowStatus($"{selectedItem.ItemName} removed from Table {SelectedTableNumber}.");
        }

        private void ChangeSelectedCartQuantity(int change)
        {
            if (CartGrid.SelectedItem is not MainCartRow selectedItem)
            {
                return;
            }

            _cartService.UpdateQuantity(SelectedTableNumber, selectedItem.ItemId, change);
            RefreshCart();
            ShowStatus($"{selectedItem.ItemName} quantity updated for Table {SelectedTableNumber}.");
        }

        private bool TryAddVisibleShortcutItem(Key key)
        {
            var index = key switch
            {
                Key.D1 or Key.NumPad1 => 0,
                Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2,
                Key.D4 or Key.NumPad4 => 3,
                Key.D5 or Key.NumPad5 => 4,
                Key.D6 or Key.NumPad6 => 5,
                Key.D7 or Key.NumPad7 => 6,
                Key.D8 or Key.NumPad8 => 7,
                Key.D9 or Key.NumPad9 => 8,
                _ => -1
            };

            if (index < 0 || index >= _visibleItems.Count)
            {
                return false;
            }

            var item = _visibleItems[index];
            _cartService.AddItem(SelectedTableNumber, item);
            RefreshCart();
            ShowStatus($"{item.Name} added using shortcut to Table {SelectedTableNumber}.");
            return true;
        }

        private bool TrySelectTableFromShortcut(Key key)
        {
            var index = key switch
            {
                Key.D1 or Key.NumPad1 => 0,
                Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2,
                Key.D4 or Key.NumPad4 => 3,
                Key.D5 or Key.NumPad5 => 4,
                _ => -1
            };

            if (index < 0 || index >= TableSelector.Items.Count)
            {
                return false;
            }

            TableSelector.SelectedIndex = index;
            return true;
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // async void event handler — wrap in try/catch to prevent silent crash
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control && TryAddVisibleShortcutItem(e.Key))
                {
                    e.Handled = true;
                    return;
                }

                if (Keyboard.Modifiers == ModifierKeys.Alt && TrySelectTableFromShortcut(e.Key))
                {
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F2)
                {
                    SearchBox.Focus();
                    SearchBox.SelectAll();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F4)
                {
                    await CheckoutCurrentTableAsync();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.F6)
                {
                    _cartService.Clear(SelectedTableNumber);
                    RefreshCart();
                    ShowStatus($"Cleared billing cart for Table {SelectedTableNumber}.");
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Enter && ItemList.IsKeyboardFocusWithin)
                {
                    AddSelectedItemToCart();
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Delete && CartGrid.SelectedItem is MainCartRow)
                {
                    RemoveSelectedCartItem();
                    e.Handled = true;
                    return;
                }

                if ((e.Key == Key.Add || e.Key == Key.OemPlus) && CartGrid.SelectedItem is MainCartRow)
                {
                    ChangeSelectedCartQuantity(1);
                    e.Handled = true;
                    return;
                }

                if ((e.Key == Key.Subtract || e.Key == Key.OemMinus) && CartGrid.SelectedItem is MainCartRow)
                {
                    ChangeSelectedCartQuantity(-1);
                    e.Handled = true;
                    return;
                }

                if (e.Key == Key.Escape && SearchBox.IsKeyboardFocusWithin)
                {
                    SearchBox.Clear();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Keyboard shortcut error: {ex.Message}");
            }
        }

        private sealed class MainCartRow
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal TaxPercentage { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal Total { get; set; }
        }
    }
}
