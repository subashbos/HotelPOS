using ClosedXML.Excel;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using HotelPOS;

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
        private readonly ISettingService _settingService;
        private readonly IReportService _reportService;
        private List<JournalRow> _allRows = new();
        private bool _isLoaded = false;   // prevents premature LoadAsync calls

        public JournalView(IOrderService orderService, ISettingService settingService, IReportService reportService)
        {
            InitializeComponent();
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _settingService = settingService ?? throw new ArgumentNullException(nameof(settingService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));

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

        private async Task RefreshTotalCountAsync()
        {
            try
            {
                var from = FromDate.SelectedDate;
                var to = ToDate.SelectedDate?.AddDays(1);
                int? tbl = GetTableFilter();

                // Get count by requesting a minimal page
                var (_, total) = await _orderService.GetPagedOrdersAsync(1, 1, from, to, tbl);
                
                JournalPager.SetExternalSource(total);
                RowCountText.Text = $"{total} transaction{(total == 1 ? "" : "s")}";
            }
            catch (Exception ex)
            {
                ShowError("Failed to refresh count", ex);
            }
        }

        private async Task LoadPagedAsync(int page, int size)
        {
            try
            {
                var from = FromDate.SelectedDate;
                var to = ToDate.SelectedDate?.AddDays(1);
                int? tbl = GetTableFilter();

                var (items, _) = await _orderService.GetPagedOrdersAsync(page, size, from, to, tbl);
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

        private int? GetTableFilter()
        {
            if (TableFilter.SelectedItem is ComboBoxItem ci && ci.Tag is string tag && int.TryParse(tag, out var tv))
                return tv;
            return null;
        }

        private void ShowError(string msg, Exception ex)
        {
             MessageBox.Show($"{msg}:\n{ex.Message}", "Journal Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("No data to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("✅  Journal exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
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
                var data = await _reportService.GetGstReportAsync(from, to);

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
                MessageBox.Show("✅  GST Report exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Print Receipt ─────────────────────────────────────────────────────
        private async void PrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int orderId)
            {
                try
                {
                    var settings = await _settingService.GetSettingsAsync();
                    var orders = await _orderService.GetAllOrdersWithItemsAsync();
                    var order = orders.FirstOrDefault(o => o.Id == orderId);

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
                    MessageBox.Show($"Failed to print receipt:\n{ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int orderId)
            {
                if (MessageBox.Show($"Are you sure you want to delete Order #{orderId}?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _orderService.DeleteOrderAsync(orderId);
                        await LoadAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Delete failed:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
