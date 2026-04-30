using ClosedXML.Excel;
using HotelPOS.Application;
using HotelPOS.Application.Interface;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public record DailyRow(DateTime Date, int OrderCount, decimal GrossRevenue, decimal GstAmount, decimal NetIncome);

    /// <summary>ViewModel for the simple bar chart.</summary>
    public class ChartBar
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public double BarHeight { get; set; }
        public double X { get; set; }
        public string ToolTipText => $"{MonthName}: Rs. {Revenue:N0}";
    }

    public partial class DashboardView : UserControl
    {
        private readonly IOrderService _orderService;
        private readonly IReportService _reportService;

        // Expose for shell-level export (kept for backward compat)
        public SalesReportDto? LastSalesReport { get; private set; }
        public List<ItemReportRowDto>? LastItemReport { get; private set; }
        public List<DailyRow>? LastDailyReport { get; private set; }

        public DashboardView(IOrderService orderService, IReportService reportService)
        {
            InitializeComponent();
            _orderService = orderService;
            _reportService = reportService;

            // Wire pagination
            TablePager.PageChanged += page => TableGrid.ItemsSource = page;
            RecentPager.PageChanged += page => RecentGrid.ItemsSource = page;
            ItemPager.PageChanged += page => ItemGrid.ItemsSource = page;
            DatePager.PageChanged += page => DateGrid.ItemsSource = page;

            Loaded += async (s, e) => await LoadAsync();
        }

        // ── Filters ───────────────────────────────────────────────────────────

        private async void Filter_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            CustomFrom.SelectedDate = null;
            CustomTo.SelectedDate = null;
            await LoadAsync();
        }

        private async void CustomDate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (CustomFrom.SelectedDate.HasValue || CustomTo.SelectedDate.HasValue)
            {
                TodayFilter.IsChecked = false;
                WeekFilter.IsChecked = false;
                AllFilter.IsChecked = false;
            }
            await LoadAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private (DateTime? from, DateTime? to) ResolveRange()
        {
            if (CustomFrom.SelectedDate.HasValue || CustomTo.SelectedDate.HasValue)
                return (CustomFrom.SelectedDate, CustomTo.SelectedDate?.AddDays(1));

            if (TodayFilter.IsChecked == true)
                return (DateTime.Today, null);

            if (WeekFilter.IsChecked == true)
            {
                var today = DateTime.Today;
                var offset = (int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1;
                return (today.AddDays(-offset), null);
            }

            return (null, null);
        }

        // ── Data loading ──────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            try
            {
                var (from, to) = ResolveRange();

                // Sales + Item reports
                var sales = await _reportService.GetSalesReportAsync(from, to);
                var items = await _reportService.GetItemReportAsync(from, to);

                LastSalesReport = sales;
                LastItemReport = items;

                RevenueValueText.Text = $"Rs. {sales.TotalRevenue:N2}";
                OrdersValueText.Text = sales.TotalOrders.ToString("N0");
                AvgValueText.Text = $"Rs. {sales.AverageOrderValue:N2}";
                TopItemText.Text = sales.MostPopularItem ?? "—";

                TablePager.SetSource(sales.SalesByTable);
                RecentPager.SetSource(sales.RecentOrders);
                SalesMixChart.ItemsSource = sales.SalesByCategory;
                ItemPager.SetSource(items);
                PaymentModeGrid.ItemsSource = sales.SalesByPaymentMode;

                // Chart
                var chartData = await _reportService.GetMonthlyChartDataAsync();
                var maxRev = chartData.Max(x => x.Revenue);
                if (maxRev == 0) maxRev = 1;

                int i = 0;
                SalesChart.ItemsSource = chartData.Select(x => new ChartBar
                {
                    MonthName = x.MonthName,
                    Revenue = x.Revenue,
                    BarHeight = (double)(x.Revenue / maxRev) * 160,
                    X = i++ * 58 + 20
                }).ToList();

                // Date-wise report — built from raw orders
                await BuildDateReport(from, to);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dashboard load failed:\n{ex.Message}", "Dashboard Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task BuildDateReport(DateTime? from, DateTime? to)
        {
            try
            {
                var allOrders = await _orderService.GetAllOrdersWithItemsAsync();

                var filtered = allOrders.AsEnumerable();
                if (from.HasValue) filtered = filtered.Where(o => o.CreatedAt.ToLocalTime() >= from.Value);
                if (to.HasValue) filtered = filtered.Where(o => o.CreatedAt.ToLocalTime() < to.Value);

                LastDailyReport = filtered
                    .GroupBy(o => o.CreatedAt.ToLocalTime().Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyRow(
                        g.Key,
                        g.Count(),
                        g.Sum(o => o.TotalAmount),
                        g.Sum(o => o.GstAmount),
                        g.Sum(o => o.Subtotal)))
                    .ToList();

                DatePager.SetSource(LastDailyReport);
            }
            catch { /* silently skip if orders aren't loaded */ }
        }

        // ── Export ────────────────────────────────────────────────────────────

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export Dashboard Report",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"HotelPOS_Report_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                // Make sure data is fresh
                await LoadAsync();

                using var wb = new XLWorkbook();

                // Sheet 1: Date-wise
                if (LastDailyReport?.Count > 0)
                {
                    var ws = wb.Worksheets.Add("Date-wise Report");
                    SetHeader(ws.Row(1));
                    ws.Cell(1, 1).Value = "Date"; ws.Cell(1, 2).Value = "Orders";
                    ws.Cell(1, 3).Value = "Gross Revenue"; ws.Cell(1, 4).Value = "Total GST"; ws.Cell(1, 5).Value = "Net Income";
                    int r = 2;
                    foreach (var d in LastDailyReport)
                    {
                        ws.Cell(r, 1).Value = d.Date.ToString("dd MMM yyyy");
                        ws.Cell(r, 2).Value = d.OrderCount;
                        ws.Cell(r, 3).Value = (double)d.GrossRevenue;
                        ws.Cell(r, 4).Value = (double)d.GstAmount;
                        ws.Cell(r, 5).Value = (double)d.NetIncome;
                        r++;
                    }
                    ws.Columns().AdjustToContents();
                }

                // Sheet 2: Sales by Table
                if (LastSalesReport?.SalesByTable.Count > 0)
                {
                    var ws2 = wb.Worksheets.Add("Sales by Table");
                    SetHeader(ws2.Row(1));
                    ws2.Cell(1, 1).Value = "Table"; ws2.Cell(1, 2).Value = "Orders"; ws2.Cell(1, 3).Value = "Revenue (Rs.)";
                    int r = 2;
                    foreach (var t in LastSalesReport.SalesByTable)
                    {
                        ws2.Cell(r, 1).Value = $"Table {t.TableNumber}";
                        ws2.Cell(r, 2).Value = t.OrderCount;
                        ws2.Cell(r, 3).Value = (double)t.TotalRevenue;
                        r++;
                    }
                    ws2.Columns().AdjustToContents();
                }

                // Sheet 3: Item Report
                if (LastItemReport?.Count > 0)
                {
                    var ws3 = wb.Worksheets.Add("Item Performance");
                    SetHeader(ws3.Row(1));
                    ws3.Cell(1, 1).Value = "Item Name"; ws3.Cell(1, 2).Value = "Qty Sold";
                    ws3.Cell(1, 3).Value = "Total Revenue (Rs.)"; ws3.Cell(1, 4).Value = "Unit Price (Rs.)";
                    int r = 2;
                    foreach (var i in LastItemReport)
                    {
                        ws3.Cell(r, 1).Value = i.ItemName;
                        ws3.Cell(r, 2).Value = i.TotalQtySold;
                        ws3.Cell(r, 3).Value = (double)i.TotalRevenue;
                        ws3.Cell(r, 4).Value = (double)i.UnitPrice;
                        r++;
                    }
                    ws3.Columns().AdjustToContents();
                }

                // Sheet 4: Payment Mode Report
                if (LastSalesReport?.SalesByPaymentMode.Count > 0)
                {
                    var ws4 = wb.Worksheets.Add("Sales by Payment Mode");
                    SetHeader(ws4.Row(1));
                    ws4.Cell(1, 1).Value = "Payment Mode"; ws4.Cell(1, 2).Value = "Orders";
                    ws4.Cell(1, 3).Value = "Total Revenue (Rs.)"; ws4.Cell(1, 4).Value = "Percentage (%)";
                    int r = 2;
                    foreach (var p in LastSalesReport.SalesByPaymentMode)
                    {
                        ws4.Cell(r, 1).Value = p.PaymentMode;
                        ws4.Cell(r, 2).Value = p.OrderCount;
                        ws4.Cell(r, 3).Value = (double)p.Revenue;
                        ws4.Cell(r, 4).Value = p.Percentage;
                        r++;
                    }
                    ws4.Columns().AdjustToContents();
                }

                wb.SaveAs(dlg.FileName);
                MessageBox.Show("✅  Report exported successfully (4 sheets).", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void SetHeader(IXLRow row)
        {
            row.Style.Font.Bold = true;
            row.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
            row.Style.Font.FontColor = XLColor.White;
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                var window = Window.GetWindow(this) as DashboardWindow;
                if (window != null)
                {
                    var order = new HotelPOS.Domain.Order
                    {
                        Id = row.OrderId,
                        TableNumber = row.TableNumber,
                        CreatedAt = row.CreatedAt,
                        Items = row.Items,
                        TotalAmount = row.Total
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
