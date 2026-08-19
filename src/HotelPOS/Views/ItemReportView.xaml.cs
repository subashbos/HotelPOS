using ClosedXML.Excel;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class ItemReportView : UserControl
    {
        private readonly INotificationService _notificationService;
        private readonly ObservableCollection<ItemSalesReportRowDto> _items = new();
        private List<ItemSalesReportRowDto> _allRows = new();
        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _isLoading;

        public ItemReportView(INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(notificationService);
            }

            ReportGrid.ItemsSource = _items;

            Loaded += async (s, e) =>
            {
                _isLoading = true;
                try
                {
                    FilterFrom.SelectedDate = DateTime.Today;
                    FilterTo.SelectedDate = DateTime.Today;
                    await LoadCategoriesAsync();
                }
                finally
                {
                    _isLoading = false;
                }
                await LoadDataAsync();
            };
        }

        /// <summary>
        /// Loads categories from a scoped category service, inserts an "All Categories" entry, and populates the category combo box.
        /// </summary>
        /// <returns>A task that completes after categories have been loaded and the combo box has been populated.</returns>
        private async Task LoadCategoriesAsync()
        {
            try
            {
                IEnumerable<Category> cats;
                using (var scope = App.CreateDbScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    cats = await categoryService.GetCategoriesAsync();
                }

                var list = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
                list.Insert(0, new Category { Id = 0, Name = "All Categories", DisplayOrder = -1 });
                ComboCategory.ItemsSource = list;
                ComboCategory.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load categories: {ex.Message}");
            }
        }

        private async void Filter_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded || _isLoading) return;
            await LoadDataAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        /// <summary>
        /// Loads item sales data using the current date range, category and search filters, one row
        /// per item sold on a single order (so each row carries its own invoice number), and updates
        /// the report grid and totals in the view.
        /// </summary>
        /// <remarks>
        /// The method fetches all orders and item catalog entries from a scoped service provider,
        /// filters orders by the selected date range and active status, projects each order's line
        /// items into one row per transaction (invoice number, date, item, category, unit price,
        /// quantity, line total), applies category and search filters, sorts by date descending
        /// (most recent sale first), assigns sequential row numbers, and binds the resulting rows to
        /// the pager and UI totals.
        /// </remarks>
        /// <returns>Completes when the report data has been loaded, processed, and bound to the UI.</returns>
        public async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var from = FilterFrom.SelectedDate;
                var to = FilterTo.SelectedDate?.AddDays(1);
                var search = SearchText.Text?.Trim();
                var categoryId = (int?)ComboCategory.SelectedValue;

                // 1. Get all orders and items catalog
                List<HotelPOS.Domain.Entities.Order> allOrders;
                List<HotelPOS.Domain.Entities.Item> allItems;
                using (var scope = App.CreateDbScope())
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                    allOrders = await orderService.GetAllOrdersWithItemsAsync();
                    allItems = await itemService.GetItemsAsync();
                }

                // 2. Filter orders by date range and active status
                var filteredOrders = allOrders.Where(o => !o.IsDeleted);
                if (from != null)
                {
                    filteredOrders = filteredOrders.Where(o => o.CreatedAt.ToLocalTime() >= from.Value);
                }
                if (to != null)
                {
                    filteredOrders = filteredOrders.Where(o => o.CreatedAt.ToLocalTime() < to.Value);
                }

                // 3. One row per order line item, carrying that order's own invoice number/date -
                // no aggregation, so every row is a real, individually traceable transaction.
                var rows = filteredOrders
                    .SelectMany(o => o.Items.Select(oi => new { Order = o, OrderItem = oi }))
                    .Select(x =>
                    {
                        var catalogItem = allItems.FirstOrDefault(it => it.Id == x.OrderItem.ItemId);
                        return new ItemSalesReportRowDto
                        {
                            InvoiceNumber = x.Order.InvoiceNumber,
                            Date = x.Order.CreatedAt.ToLocalTime(),
                            ItemId = x.OrderItem.ItemId,
                            ItemName = catalogItem?.Name ?? x.OrderItem.ItemName,
                            CategoryId = catalogItem?.CategoryId ?? 0,
                            CategoryName = catalogItem?.Category?.Name ?? "General",
                            UnitPrice = x.OrderItem.Price,
                            Quantity = x.OrderItem.Quantity,
                            LineTotal = x.OrderItem.Total
                        };
                    })
                    .ToList();

                // 4. Apply category filter
                if (categoryId != null && categoryId > 0)
                {
                    rows = rows.Where(x => x.CategoryId == categoryId).ToList();
                }

                // 5. Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    rows = rows.Where(x => x.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 6. Sort by date descending - most recent sale first, since this is now a
                // transaction list rather than a per-item ranking.
                rows = rows.OrderByDescending(x => x.Date).ToList();

                // 7. Assign SNo
                for (int i = 0; i < rows.Count; i++)
                {
                    rows[i].SNo = i + 1;
                }

                // 8. Bind and update totals
                _allRows = rows;
                _items.Clear();
                _currentPage = 1;
                LoadMore();
                TotalQtySold.Text = rows.Sum(x => x.Quantity).ToString();
                TotalRevenueSum.Text = $"Rs. {rows.Sum(x => x.LineTotal):N2}";
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load item report: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadMore()
        {
            var itemsToLoad = _allRows.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            if (itemsToLoad.Any())
            {
                foreach (var i in itemsToLoad) _items.Add(i);
                _currentPage++;
            }
        }

        private void ReportGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                LoadMore();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var items = _allRows;
            if (items == null || !items.Any())
            {
                _notificationService.ShowWarning("No data to export.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Item_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Item Report");

                    // Headers
                    ws.Cell(1, 1).Value = "S.No";
                    ws.Cell(1, 2).Value = "Invoice";
                    ws.Cell(1, 3).Value = "Date";
                    ws.Cell(1, 4).Value = "Item Name";
                    ws.Cell(1, 5).Value = "Category";
                    ws.Cell(1, 6).Value = "Unit Price";
                    ws.Cell(1, 7).Value = "Quantity";
                    ws.Cell(1, 8).Value = "Line Total";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                    headerRow.Style.Font.FontColor = XLColor.White;

                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cell(row, 1).Value = item.SNo;
                        ws.Cell(row, 2).Value = (item.InvoiceNumber ?? "").ForSpreadsheet();
                        ws.Cell(row, 3).Value = item.Date.ToString("dd MMM yyyy hh:mm tt");
                        ws.Cell(row, 4).Value = item.ItemName.ForSpreadsheet();
                        ws.Cell(row, 5).Value = item.CategoryName.ForSpreadsheet();
                        ws.Cell(row, 6).Value = (double)item.UnitPrice;
                        ws.Cell(row, 7).Value = item.Quantity;
                        ws.Cell(row, 8).Value = (double)item.LineTotal;
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                    _notificationService.ShowSuccess("Item report exported successfully.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Export failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>One row = one item sold on one order. Deliberately not aggregated across orders,
    /// so every row can carry its own invoice number.</summary>
    public class ItemSalesReportRowDto
    {
        public int SNo { get; set; }
        public string? InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
