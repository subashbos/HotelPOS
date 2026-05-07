using ClosedXML.Excel;
using HotelPOS.Application;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HotelPOS.Views
{
    public partial class ItemView : UserControl
    {
        private readonly IItemService _itemService;
        private readonly ICategoryService _categoryService;
        private List<Item> _allItems = new();
        private List<Item> _filtered = new();

        private static readonly SolidColorBrush SuccessBg = new(Color.FromRgb(0xD4, 0xED, 0xDA));
        private static readonly SolidColorBrush SuccessFg = new(Color.FromRgb(0x15, 0x57, 0x24));
        private static readonly SolidColorBrush ErrorBg = new(Color.FromRgb(0xF8, 0xD7, 0xDA));
        private static readonly SolidColorBrush ErrorFg = new(Color.FromRgb(0x72, 0x1C, 0x24));

        public ItemView(IItemService itemService, ICategoryService categoryService)
        {
            InitializeComponent();
            _itemService = itemService;
            _categoryService = categoryService;
            ItemPager.PageChanged += page => ItemGrid.ItemsSource = page;
            Loaded += async (s, e) => await LoadDataAsync();
        }

        // ── Data ──────────────────────────────────────────────────────────────

        private async Task LoadDataAsync()
        {
            try
            {
                _allItems = await _itemService.GetItemsAsync();
                ApplyFilter();
                TotalCountBadge.Text = _allItems.Count.ToString();
            }
            catch (Exception ex) { ShowStatus(ex.Message, false); }
        }

        private void ApplyFilter()
        {
            var q = SearchBox.Text.Trim();
            _filtered = string.IsNullOrWhiteSpace(q)
                ? _allItems.OrderBy(i => i.Name).ToList()
                : _allItems.Where(i => i.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                      (i.Category?.Name != null && i.Category.Name.Contains(q, StringComparison.OrdinalIgnoreCase)))
                           .OrderBy(i => i.Name).ToList();

            for (int i = 0; i < _filtered.Count; i++) _filtered[i].SNo = i + 1;

            ItemPager.SetSource(_filtered);
            FilteredCountBadge.Text = _filtered.Count.ToString();
            ClearSearch.Visibility = string.IsNullOrWhiteSpace(q) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            SearchBox.Focus();
        }

        // ── Add / Edit / Delete ───────────────────────────────────────────────

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var sp = ((App)System.Windows.Application.Current).ServiceProvider;
            var popup = sp.GetRequiredService<AddItemWindow>();
            popup.Owner = Window.GetWindow(this);
            popup.ItemSaved += () => _ = LoadDataAsync();
            popup.ShowDialog();

            _ = LoadDataAsync();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is Item item)
            {
                var sp = ((App)System.Windows.Application.Current).ServiceProvider;
                var popup = sp.GetRequiredService<AddItemWindow>();
                popup.Owner = Window.GetWindow(this);
                popup.LoadItemForEdit(item);
                popup.ItemSaved += () => _ = LoadDataAsync();
                popup.ShowDialog();

                _ = LoadDataAsync();
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                var item = _allItems.FirstOrDefault(i => i.Id == id);
                if (MessageBox.Show($"Delete '{item?.Name}'?\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await _itemService.DeleteItemAsync(id);
                    await LoadDataAsync();
                    ShowStatus($"🗑  Item deleted.", true);
                }
            }
        }

        // ── Excel Import ──────────────────────────────────────────────────────

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Excel File",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var preview = ReadExcel(dlg.FileName);
                if (preview.Count == 0)
                {
                    MessageBox.Show("No valid rows found. Ensure the file has 'Name' and 'Price' columns.",
                        "Nothing to Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var msg = $"Found {preview.Count} item(s) to import:\n\n" +
                          string.Join("\n", preview.Take(10).Select(p => $"  • {p.Name}  —  Rs. {p.Price:N2}")) +
                          (preview.Count > 10 ? $"\n  … and {preview.Count - 10} more" : "") +
                          "\n\nProceed with import?";

                if (MessageBox.Show(msg, "Preview Import", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    != MessageBoxResult.Yes) return;

                var (added, skipped) = await _itemService.BulkAddAsync(preview);
                await LoadDataAsync();
                ShowStatus($"✅  Import complete: {added} added, {skipped} skipped.", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Import error: {ex.Message}", false);
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export Item Catalog",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Menu_Export_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Menu Items");
                ws.Cell(1, 1).Value = "Name";
                ws.Cell(1, 2).Value = "Price";
                ws.Cell(1, 3).Value = "Tax";
                ws.Cell(1, 4).Value = "Category";

                var header = ws.Row(1);
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                header.Style.Font.FontColor = XLColor.White;

                for (int i = 0; i < _allItems.Count; i++)
                {
                    var item = _allItems[i];
                    ws.Cell(i + 2, 1).Value = item.Name;
                    ws.Cell(i + 2, 2).Value = item.Price;
                    ws.Cell(i + 2, 3).Value = item.TaxPercentage;
                    ws.Cell(i + 2, 4).Value = item.Category?.Name ?? "";
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
                ShowStatus("✅  Catalog exported successfully.", true);
            }
            catch (Exception ex) { ShowStatus($"Export error: {ex.Message}", false); }
        }

        private List<CreateItemDto> ReadExcel(string path)
        {
            var result = new List<CreateItemDto>();
            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);
            var headers = ws.Row(1).CellsUsed()
                            .ToDictionary(c => c.Value.ToString().Trim().ToLowerInvariant(), c => c.Address.ColumnNumber);

            if (!headers.TryGetValue("name", out int nameCol) ||
                !headers.TryGetValue("price", out int priceCol))
                throw new Exception("Excel must have 'Name' and 'Price' column headers in row 1.");

            headers.TryGetValue("tax", out int taxCol);
            headers.TryGetValue("category", out int catCol);

            // Fetch categories for mapping
            var categories = _categoryService.GetCategoriesAsync().GetAwaiter().GetResult();

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                var name = row.Cell(nameCol).GetString().Trim();
                var raw = row.Cell(priceCol).GetString().Trim();
                if (!decimal.TryParse(raw, out var price)) continue;
                if (string.IsNullOrWhiteSpace(name) || price <= 0) continue;

                decimal tax = 0;
                if (taxCol > 0)
                {
                    var taxRaw = row.Cell(taxCol).GetString().Trim();
                    decimal.TryParse(taxRaw, out tax);
                }

                int? catId = null;
                if (catCol > 0)
                {
                    var catName = row.Cell(catCol).GetString().Trim();
                    if (!string.IsNullOrEmpty(catName))
                    {
                        catId = categories.FirstOrDefault(c => string.Equals(c.Name, catName, StringComparison.OrdinalIgnoreCase))?.Id;
                    }
                }

                result.Add(new CreateItemDto { Name = name, Price = price, TaxPercentage = tax, CategoryId = catId });
            }
            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ShowStatus(string message, bool success)
        {
            // Status bar is now removed or we can use a Toast or MessageBox
            // For now, let's just use MessageBox if it's an error
            if (!success)
                MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
