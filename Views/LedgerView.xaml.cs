using ClosedXML.Excel;
using HotelPOS.Application.Interface;
using HotelPOS.Domain;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public class LedgerRow
    {
        public int SNo { get; set; }
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal GstAmount { get; set; }
        public decimal NetIncome { get; set; }
        public decimal RunningBalance { get; set; }

        public LedgerRow(DateTime date, int count, decimal gross, decimal gst, decimal net, decimal balance)
        {
            Date = date;
            OrderCount = count;
            GrossRevenue = gross;
            GstAmount = gst;
            NetIncome = net;
            RunningBalance = balance;
        }
    }

    public partial class LedgerView : UserControl
    {
        private readonly IOrderService _orderService;
        private List<LedgerRow> _allRows = new();
        private bool _isLoaded = false;

        public LedgerView(IOrderService orderService)
        {
            InitializeComponent();
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            LedgerPager.PageChanged += page => LedgerGrid.ItemsSource = page;

            Loaded += async (s, e) =>
            {
                _isLoaded = true;
                await LoadAsync();
            };
        }

        private async Task LoadAsync()
        {
            if (!_isLoaded) return;

            try
            {
                DateTime? from = FromDate.SelectedDate;
                DateTime? to = ToDate.SelectedDate?.AddDays(1);

                var allOrders = await _orderService.GetAllOrdersWithItemsAsync();
                var orders = allOrders.AsEnumerable();
                if (from.HasValue) orders = orders.Where(o => o.CreatedAt.ToLocalTime() >= from.Value);
                if (to.HasValue) orders = orders.Where(o => o.CreatedAt.ToLocalTime() <= to.Value);
                BuildLedger(orders.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ledger Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuildLedger(List<Order> orders)
        {
            const decimal gstRate = 0.05m;
            decimal running = 0;
            _allRows = orders
                .GroupBy(o => o.CreatedAt.ToLocalTime().Date)
                .OrderBy(g => g.Key)
                .Select((g, idx) =>
                {
                    var gross = g.Sum(o => o.TotalAmount);
                    var net = Math.Round(gross / (1 + gstRate), 2);
                    var gst = Math.Round(gross - net, 2);
                    running += gross;
                    var row = new LedgerRow(g.Key, g.Count(), gross, gst, net, running);
                    row.SNo = idx + 1;
                    return row;
                })
                .ToList();

            TotalRevBadge.Text = $"Rs. {_allRows.Sum(r => r.GrossRevenue):N2}";
            TotalOrdersBadge.Text = _allRows.Sum(r => r.OrderCount).ToString();
            LedgerPager.SetSource(_allRows);
        }

        // ── Toolbar events ────────────────────────────────────────────────────

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            await LoadAsync();
        }

        private async void AllTime_Click(object sender, RoutedEventArgs e)
        {
            FromDate.SelectedDate = null;
            ToDate.SelectedDate = null;
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
                Title = "Save Ledger as Excel",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Ledger_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Revenue Ledger");

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                ws.Row(1).Style.Font.FontColor = XLColor.White;

                var headers = new[] { "Date", "Orders", "Gross Revenue", "GST Collected", "Net Income", "Running Balance" };
                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];

                int row = 2;
                foreach (var r in _allRows)
                {
                    ws.Cell(row, 1).Value = r.Date.ToString("dd MMM yyyy");
                    ws.Cell(row, 2).Value = r.OrderCount;
                    ws.Cell(row, 3).Value = (double)r.GrossRevenue;
                    ws.Cell(row, 4).Value = (double)r.GstAmount;
                    ws.Cell(row, 5).Value = (double)r.NetIncome;
                    ws.Cell(row, 6).Value = (double)r.RunningBalance;
                    row++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(dlg.FileName);
                MessageBox.Show("✅  Ledger exported successfully.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error:\n{ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
