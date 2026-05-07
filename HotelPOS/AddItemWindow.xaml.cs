using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS
{
    public partial class AddItemWindow : Window
    {
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private Item? _editingItem;
        public event Action? ItemSaved;

        public AddItemWindow(IItemService itemService, ICategoryService categoryService)
        {
            InitializeComponent();
            _itemService = itemService;
            _categoryService = categoryService;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var cats = await _categoryService.GetCategoriesAsync();
                ItemCategoryCombo.ItemsSource = cats;

                if (_editingItem != null)
                {
                    ItemNameBox.Text = _editingItem.Name;
                    ItemPriceBox.Text = _editingItem.Price.ToString("F2");
                    ItemCategoryCombo.SelectedValue = _editingItem.CategoryId;
                    SetTaxCombo(_editingItem.TaxPercentage);
                    BarcodeBox.Text = _editingItem.Barcode;
                    TrackStockCheck.IsChecked = _editingItem.TrackInventory;
                    StockQuantityBox.Text = _editingItem.StockQuantity.ToString();
                    FormTitle.Text = "Edit Menu Item";
                    SubmitBtn.Content = "💾  Save Changes";
                }
            }
            catch (Exception ex) { ShowStatus(ex.Message, true); }

            ItemNameBox.Focus();
        }

        public void LoadItemForEdit(Item item)
        {
            _editingItem = item;
        }

        private void SetTaxCombo(decimal rate)
        {
            foreach (ComboBoxItem item in TaxCombo.Items)
            {
                if (decimal.TryParse(item.Tag?.ToString(), out var r) && r == rate)
                {
                    TaxCombo.SelectedItem = item;
                    return;
                }
            }
            TaxCombo.SelectedIndex = 0;
        }

        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = ItemNameBox.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) { ShowStatus("Item name is required.", true); return; }

                if (!decimal.TryParse(ItemPriceBox.Text?.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var price))
                {
                    ShowStatus("Enter a valid price — e.g. 120.50", isError: true);
                    ItemPriceBox.Focus();
                    return;
                }

                int.TryParse(StockQuantityBox.Text?.Trim(), out int stock);

                decimal tax = 0;
                if (TaxCombo.SelectedItem is ComboBoxItem cbi && decimal.TryParse(cbi.Tag?.ToString(), out var t))
                    tax = t;

                var catId = (int?)ItemCategoryCombo.SelectedValue;

                var dto = new CreateItemDto
                {
                    Name = name,
                    Price = price,
                    TaxPercentage = tax,
                    CategoryId = catId,
                    StockQuantity = stock,
                    TrackInventory = TrackStockCheck.IsChecked ?? false,
                    Barcode = BarcodeBox.Text?.Trim()
                };

                if (_editingItem == null)
                {
                    await _itemService.AddItemAsync(dto);
                    ItemSaved?.Invoke();
                    if (MessageBox.Show($"✓ '{name}' saved successfully. Add another?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        ItemNameBox.Clear(); ItemPriceBox.Clear();
                        ItemCategoryCombo.SelectedIndex = -1; TaxCombo.SelectedIndex = 0;
                        ItemNameBox.Focus();
                        StatusBorder.Visibility = Visibility.Collapsed;
                    }
                    else { Close(); }
                }
                else
                {
                    await _itemService.UpdateItemAsync(_editingItem.Id, dto);
                    ItemSaved?.Invoke();
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", isError: true);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private static readonly SolidColorBrush _errorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush _errorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));
        private static readonly SolidColorBrush _infoBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush _infoFg = new(Color.FromRgb(0x15, 0x57, 0x24));

        private void ShowStatus(string message, bool isError)
        {
            StatusText.Text = message;
            StatusBorder.Background = isError ? _errorBg : _infoBg;
            StatusText.Foreground = isError ? _errorFg : _infoFg;
            StatusBorder.Visibility = Visibility.Visible;
        }
    }
}
