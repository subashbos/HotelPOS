using ClosedXML.Excel;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    /// <summary>Journal row — wraps Order with computed GST column for the grid.</summary>
    public class JournalRow
    {
        public int SNo { get; set; }
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public List<OrderItem> Items { get; set; } = new();
        public int ItemCount => Items?.Count ?? 0;
    }

    public partial class JournalView : UserControl
    {
        private readonly IOrderService _orderService;
        private readonly IReportService _reportService;
        private readonly INotificationService _notificationService;
        private List<JournalRow> _allRows = new();
        private bool _isLoaded = false;   // prevents premature LoadAsync calls

        public JournalView(IOrderService orderService, IReportService reportService, INotificationService notificationService)
        {
            InitializeComponent();
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(orderService);
                App.RegisterTestService(reportService);
                App.RegisterTestService(notificationService);
            }

            // Wire pager for server-side pagination
            JournalPager.ExternalPageRequested += async (page, size) => await LoadPagedAsync(page, size);

            Loaded += async (s, e) =>
            {
                _isLoaded = true;
                await LoadAsync();
            };
        }

        private async Task LoadAsync()
        {
            if (!_isLoaded) return;
            JournalPager.ResetToFirstPage();
            await RefreshTotalCountAsync();
        }

        /// <summary>
        /// Updates the journal pager's total record count and the row count text based on the currently selected date range and table filter.
        /// </summary>
        /// <remarks>
        /// Reads the FromDate and ToDate controls (ToDate is treated as exclusive by adding one day) and the optional table filter, requests the total count from the order service, sets JournalPager's external source to that total, and updates RowCountText with a human-readable transaction count. If an error occurs, it reports it via ShowError.
        /// </remarks>
        private async Task RefreshTotalCountAsync()
        {
            using (var scope = App.CreateDbScope())
            {
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                try
                {
                    var from = FromDate.SelectedDate;
                    var to = ToDate.SelectedDate?.AddDays(1);
                    int? tbl = GetTableFilter();

                    // Get count by requesting a minimal page
                    var result = await orderService.GetPagedOrdersAsync(new PagedOrdersRequest(1, 1, from, to, tbl));
                    var total = result.TotalCount;

                    JournalPager.SetExternalSource(total);
                    RowCountText.Text = $"{total} transaction{(total == 1 ? "" : "s")}";
                }
                catch (Exception ex)
                {
                    ShowError("Failed to refresh count", ex);
                }
            }
        }

        /// <summary>
        /// Loads a single page of orders using the current date and table filters and sets the JournalGrid's ItemsSource to the resulting list of JournalRow entries.
        /// </summary>
        /// <param name="page">One-based page index to load.</param>
        /// <param name="size">Number of orders per page.</param>
        /// <remarks>On failure the method reports the error via ShowError and does not propagate the exception.</remarks>
        private async Task LoadPagedAsync(int page, int size)
        {
            using (var scope = App.CreateDbScope())
            {
                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                try
                {
                    var from = FromDate.SelectedDate;
                    var to = ToDate.SelectedDate?.AddDays(1);
                    int? tbl = GetTableFilter();

                    var result = await orderService.GetPagedOrdersAsync(new PagedOrdersRequest(page, size, from, to, tbl));
                    var items = result.Items;
                    int startSno = (page - 1) * size + 1;

                    JournalGrid.ItemsSource = items
                        .Select((o, idx) => new JournalRow
                        {
                            SNo = startSno + idx,
                            Id = o.Id,
                            TableNumber = o.TableNumber,
                            CreatedAt = o.CreatedAt,
                            TotalAmount = o.TotalAmount,
                            DiscountAmount = o.DiscountAmount,
                            PaymentMode = o.PaymentMode,
                            Items = o.Items ?? new List<OrderItem>()
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    ShowError("Failed to load page", ex);
                }
            }
        }

        private int? GetTableFilter()
        {
            if (TableFilter.SelectedItem is ComboBoxItem ci && ci.Tag is string tag && int.TryParse(tag, out var tv))
                return tv;
            return null;
        }

        private void ShowError(string msg, Exception ex)
        {
            _notificationService.ShowError($"{msg}: {ex.Message}");
        }

        // ── Toolbar events ────────────────────────────────────────────────────

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Guard: SelectionChanged fires during InitializeComponent (XAML parsing).
            // Skip until the Loaded event has run.
            if (!_isLoaded) return;
            await LoadAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        // ── Export ────────────────────────────────────────────────────────────

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0)
            {
                _notificationService.ShowInfo("No data to export.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Save Journal as Excel",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Journal_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Transaction Journal");

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                string[] headers = { "Order #", "Date & Time", "Table", "Items", "Total Amount" };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cell(1, c + 1).Value = headers[c];

                int row = 2;
                foreach (var r in _allRows)
                {
                    ws.Cell(row, 1).Value = r.Id;
                    ws.Cell(row, 2).Value = r.CreatedAt.ToString("dd MMM yyyy HH:mm");
                    ws.Cell(row, 3).Value = $"Table {r.TableNumber}";
                    ws.Cell(row, 4).Value = r.ItemCount;
                    ws.Cell(row, 5).Value = (double)r.TotalAmount;
                    row++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
                _notificationService.ShowSuccess("Journal exported successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Export error: {ex.Message}");
            }
        }
        /// <summary>
        /// Exports a GST report for the selected date range to an Excel file and prompts the user to save it.
        /// </summary>
        /// <remarks>
        /// If FromDate or ToDate are not selected, the method defaults the range to the last 30 days (FromDate = today - 30 days, ToDate = today).
        /// The report is retrieved from IReportService and written to an .xlsx workbook with columns: Date, Orders, Gross Revenue, GST (5%), Net Revenue.
        /// Success or failure is reported via the notification service.
        /// </remarks>
        /// <param name="sender">The source of the event (typically the export button).</param>
        /// <param name="e">Event data for the click event.</param>
        private async void ExportGst_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save GST Report as Excel",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"GST_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var from = FromDate.SelectedDate ?? DateTime.Today.AddDays(-30);
                var to = ToDate.SelectedDate ?? DateTime.Today;

                List<GstReportRowDto> data;
                using (var scope = App.CreateDbScope())
                {
                    var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
                    data = await reportService.GetGstReportAsync(from, to);
                }

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("GST Report");

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                string[] headers = { "Date", "Orders", "Gross Revenue", "GST (5%)", "Net Revenue" };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cell(1, c + 1).Value = headers[c];

                int r = 2;
                foreach (var d in data)
                {
                    ws.Cell(r, 1).Value = d.Date.ToString("dd MMM yyyy");
                    ws.Cell(r, 2).Value = d.OrderCount;
                    ws.Cell(r, 3).Value = (double)d.GrossRevenue;
                    ws.Cell(r, 4).Value = (double)d.GstAmount;
                    ws.Cell(r, 5).Value = (double)d.NetIncome;
                    r++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
                _notificationService.ShowSuccess("GST Report exported successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Export error: {ex.Message}");
            }
        }

        /// <summary>
        /// Prints or previews the receipt for the order identified by the sender Button's Tag.
        /// </summary>
        /// <param name="sender">The event source; expected to be a <see cref="System.Windows.Controls.Button"/> whose <c>Tag</c> contains the order ID (an <c>int</c>).</param>
        /// <param name="e">Event arguments for the routed event.</param>
        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int orderId)
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
                        var orders = await orderService.GetAllOrdersWithItemsAsync();
                        order = orders.FirstOrDefault(o => o.Id == orderId);
                    }

                    if (order == null) return;

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
                            dialog.PrintQueue = new System.Printing.PrintServer().GetPrintQueue(settings.DefaultPrinter);
                        dialog.PrintDocument(((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator, "Receipt " + order.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Failed to print receipt: {ex.Message}");
                }
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is JournalRow row)
            {
                // Find parent DashboardWindow
                var window = Window.GetWindow(this) as DashboardWindow;
                if (window != null)
                {
                    var order = new Order
                    {
                        Id = row.Id,
                        TableNumber = row.TableNumber,
                        CreatedAt = row.CreatedAt,
                        Items = row.Items,
                        TotalAmount = row.TotalAmount,
                        DiscountAmount = row.DiscountAmount,
                        PaymentMode = row.PaymentMode
                    };
                    window.StartEditOrder(order);
                }
            }
        }

        /// <summary>
        /// Prompts the user to confirm deletion of the order identified by the clicked button's Tag and, if confirmed, deletes that order using a scoped IOrderService and reloads the journal view.
        /// </summary>
        /// <param name="sender">The Button whose Tag contains the order ID to delete.</param>
        /// <param name="e">Click event data.</param>
        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int orderId)
            {
                if (MessageBox.Show($"Are you sure you want to delete Order #{orderId}?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var scope = App.CreateDbScope())
                        {
                            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                            await orderService.DeleteOrderAsync(orderId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"Delete failed: {ex.Message}");
                    }
                    await LoadAsync();
                }
            }
        }
    }
}
