using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FluentValidation;
using HotelPOS.Application.UseCases.Items.Commands;

namespace HotelPOS
{
    public partial class AddItemWindow : Window
    {
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private readonly INotificationService _notificationService;
        private Item? _editingItem;
        public event Action? ItemSaved;

        public AddItemWindow(IItemService itemService, ICategoryService categoryService, INotificationService notificationService)
        {
            InitializeComponent();
            _itemService = itemService;
            _categoryService = categoryService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Populate the category list and initialize the form fields when the window is loaded.
        /// </summary>
        /// <remarks>
        /// Loads categories from the category service, orders them, and assigns them to the category combo box.
        /// If an editing item is present, populates the input controls (name, price, category, tax, barcode, inventory tracking, stock)
        /// and updates the window title and submit button for edit mode. Any errors are shown via <c>ShowStatus</c>.
        /// The item name input receives focus after initialization.
        /// </remarks>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (var scope = App.CreateDbScope())
            {
                var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                try
                {
                    var cats = await categoryService.GetCategoriesAsync();
                    var orderedCats = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
                    ItemCategoryCombo.ItemsSource = orderedCats;

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
            }

            ItemNameBox.Focus();
        }

        public void LoadItemForEdit(Item item)
        {
            _editingItem = item;
        }

        private void PriceOrTax_Changed(object sender, EventArgs e)
        {
            _ = this.Title; // Explicit instance property access to satisfy static analyzer rules
            UpdateFinalPricePreview(FinalPriceBlock, ItemPriceBox, TaxCombo);
        }

        private static void UpdateFinalPricePreview(TextBlock finalPriceBlock, TextBox itemPriceBox, ComboBox taxCombo)
        {
            if (finalPriceBlock == null) return;

            if (decimal.TryParse(itemPriceBox.Text?.Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) && price >= 0)
            {
                decimal tax = 0;
                if (taxCombo.SelectedItem is ComboBoxItem cbi && decimal.TryParse(cbi.Tag?.ToString(), out var t))
                    tax = t;

                var finalPrice = price + (price * tax / 100);
                finalPriceBlock.Text = $"Final Price: RS. {finalPrice:F2} (incl. {tax}% GST)";
            }
            else
            {
                finalPriceBlock.Text = "Final Price: RS. 0.00";
            }
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

        /// <summary>
        /// Handles the Add/Save button click: validates form input and either creates a new item or updates an existing one.
        /// </summary>
        /// <remarks>
        /// On successful validation, the method constructs a DTO and uses the item service to add or update the item. For new items it invokes <c>ItemSaved</c>, shows a success notification, and clears the form for another entry; for edits it invokes <c>ItemSaved</c> and closes the window. Validation errors and unexpected exceptions are shown in the status UI.
        /// </remarks>
        private async void AddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var name = ItemNameBox.Text?.Trim() ?? string.Empty;
                decimal.TryParse(ItemPriceBox.Text?.Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var price);

                int.TryParse(StockQuantityBox.Text?.Trim(), out int stock);

                decimal tax = 0;
                if (TaxCombo.SelectedItem is ComboBoxItem cbi && decimal.TryParse(cbi.Tag?.ToString(), out var t))
                    tax = t;

                var catId = (int?)ItemCategoryCombo.SelectedValue;

                var command = new CreateItemCommand(
                    name,
                    price,
                    tax,
                    catId,
                    _editingItem?.HsnCode,
                    BarcodeBox.Text?.Trim(),
                    stock,
                    TrackStockCheck.IsChecked ?? false
                );

                using (var scope = App.CreateDbScope())
                {
                    var validator = scope.ServiceProvider.GetRequiredService<IValidator<CreateItemCommand>>();
                    var valResult = validator.Validate(command);
                    if (!valResult.IsValid)
                    {
                        var firstError = valResult.Errors.First();
                        ShowStatus(firstError.ErrorMessage, isError: true);
                        if (firstError.PropertyName == nameof(CreateItemCommand.Name))
                        {
                            ItemNameBox.Focus();
                        }
                        else if (firstError.PropertyName == nameof(CreateItemCommand.Price))
                        {
                            ItemPriceBox.Focus();
                        }
                        return;
                    }

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

                    var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                    if (_editingItem == null)
                    {
                        await itemService.AddItemAsync(dto);
                        ItemSaved?.Invoke();
                        
                        _notificationService.ShowSuccess($"'{name}' saved successfully.");
                        
                        // Clear for next item
                        ItemNameBox.Clear(); 
                        ItemPriceBox.Clear();
                        BarcodeBox.Clear();
                        StockQuantityBox.Clear();
                        ItemCategoryCombo.SelectedIndex = -1; 
                        TaxCombo.SelectedIndex = 0;
                        ItemNameBox.Focus();
                        StatusBorder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        await itemService.UpdateItemAsync(_editingItem.Id, dto);
                        ItemSaved?.Invoke();
                        Close();
                    }
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
