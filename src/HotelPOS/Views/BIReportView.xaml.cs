using ClosedXML.Excel;
using HotelPOS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HotelPOS.Views
{
    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double margin)
            {
                if (margin < 10) return "Low";
                if (margin < 25) return "Mid";
                return "High";
            }
            return "High";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BIChartBar
    {
        public string MonthName { get; set; } = string.Empty;
        public double BarHeightRevenue { get; set; }
        public double BarHeightProfit { get; set; }
        public double X { get; set; }
        public double X1 => X + 18;
        public string ToolTipRevenue { get; set; } = string.Empty;
        public string ToolTipProfit { get; set; } = string.Empty;
    }

    public class BIWastageReasonRow
    {
        public string Reason { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
        public System.Windows.Media.Brush FillColor { get; set; } = System.Windows.Media.Brushes.Gray;
    }

    public partial class BIReportView : UserControl
    {
        private readonly IBIReportService _biService;
        private readonly IItemService _itemService;
        private readonly INotificationService _notificationService;
        private bool _isLoading;

        private readonly string[] _colors = { "#4facfe", "#2ecc71", "#f39c12", "#e74c3c" };

        public BIReportView(IBIReportService biService, IItemService itemService, INotificationService notificationService)
        {
            InitializeComponent();
            _biService = biService;
            _itemService = itemService;
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(biService);
                App.RegisterTestService(itemService);
                App.RegisterTestService(notificationService);
            }

            // Set default date range to last 30 days
            GlobalFrom.SelectedDate = DateTime.Today.AddDays(-30);
            GlobalTo.SelectedDate = DateTime.Today;

            Loaded += async (s, e) =>
            {
                await LoadItemsAsync();
                await LoadDataAsync();
            };
        }

        private async Task LoadItemsAsync()
        {
            try
            {
                using var scope = App.CreateDbScope();
                var itemService = scope.ServiceProvider.GetRequiredService<IItemService>();
                var items = await itemService.GetItemsAsync();
                ComboWasteItem.ItemsSource = items.OrderBy(i => i.Name).ToList();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load items: {ex.Message}");
            }
        }

        public async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                DateTime? from = GlobalFrom.SelectedDate;
                DateTime? to = GlobalTo.SelectedDate?.Date.AddDays(1).AddSeconds(-1);

                // 1. Load Profit margin metrics
                var profitSummary = await _biService.GetProfitMarginSummaryAsync(from, to);
                TxtTotalRevenue.Text = $"Rs. {profitSummary.TotalRevenue:N2}";
                TxtNetProfit.Text = $"Rs. {profitSummary.NetProfit:N2}";
                TxtFoodCostPct.Text = $"{profitSummary.FoodCostPercentage:N1}%";

                // 2. Load Item margin grid
                var itemMargins = await _biService.GetItemMarginsAsync(from, to);
                MarginsGrid.ItemsSource = itemMargins;

                // 3. Load Wastage summary & grid
                var wastageSummary = await _biService.GetWastageSummaryAsync(from, to);
                TxtWastageCost.Text = $"Rs. {wastageSummary.TotalWastageCost:N2}";
                WastageGrid.ItemsSource = wastageSummary.RecentWastage;

                // Populate wastage reason categories
                int colorIdx = 0;
                var reasonRows = wastageSummary.ReasonsBreakdown.Select(r => new BIWastageReasonRow
                {
                    Reason = r.Reason,
                    Quantity = r.Quantity,
                    Cost = r.Cost,
                    FillColor = (System.Windows.Media.Brush?)new System.Windows.Media.BrushConverter().ConvertFrom(_colors[colorIdx++ % _colors.Length]) ?? System.Windows.Media.Brushes.Gray
                }).ToList();
                WastageReasonsList.ItemsSource = reasonRows;

                // 4. Load Low stock alerts
                var lowStock = await _biService.GetLowStockAlertsAsync();
                LowStockGrid.ItemsSource = lowStock;

                // 5. Load Monthly Chart Trends
                var trendData = await _biService.GetMonthlyTrendDataAsync();
                double maxVal = (double)trendData.Max(t => Math.Max(t.Revenue, t.GrossProfit));
                if (maxVal <= 0) maxVal = 1;

                int i = 0;
                MonthlyChart.ItemsSource = trendData.Select(t => new BIChartBar
                {
                    MonthName = t.MonthName,
                    BarHeightRevenue = (double)t.Revenue / maxVal * 150.0,
                    BarHeightProfit = (double)t.GrossProfit / maxVal * 150.0,
                    X = i++ * 50 + 20,
                    ToolTipRevenue = $"Revenue: Rs. {t.Revenue:N2}",
                    ToolTipProfit = $"Gross Profit: Rs. {t.GrossProfit:N2}"
                }).ToList();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load BI analytics: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            await LoadDataAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async void LogWastage_Click(object sender, RoutedEventArgs e)
        {
            if (ComboWasteItem.SelectedValue == null)
            {
                _notificationService.ShowError("Please select an item");
                return;
            }

            if (!int.TryParse(TxtWasteQty.Text, out int qty) || qty <= 0)
            {
                _notificationService.ShowError("Please enter a valid positive quantity");
                return;
            }

            string reason = (ComboWasteReason.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Spoilage";
            string? notes = string.IsNullOrWhiteSpace(TxtWasteNotes.Text) ? null : TxtWasteNotes.Text;
            int itemId = (int)ComboWasteItem.SelectedValue;

            try
            {
                await _biService.LogWastageAsync(itemId, qty, reason, notes);
                _notificationService.ShowSuccess("Wastage logged successfully.");
                TxtWasteQty.Clear();
                TxtWasteNotes.Clear();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Log wastage failed: {ex.Message}");
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var items = MarginsGrid.ItemsSource as List<ItemMarginRowDto>;
            if (items == null || items.Count == 0)
            {
                _notificationService.ShowError("No margin data available to export");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Export Margins Report",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"HotelPOS_BI_Margins_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Item Profitability");

                    ws.Cell(1, 1).Value = "Item Name";
                    ws.Cell(1, 2).Value = "Category";
                    ws.Cell(1, 3).Value = "Quantity Sold";
                    ws.Cell(1, 4).Value = "Sell Price";
                    ws.Cell(1, 5).Value = "Cost Price";
                    ws.Cell(1, 6).Value = "Revenue";
                    ws.Cell(1, 7).Value = "COGS";
                    ws.Cell(1, 8).Value = "Profit";
                    ws.Cell(1, 9).Value = "Margin %";
                    ws.Cell(1, 10).Value = "Recommendation";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                    headerRow.Style.Font.FontColor = XLColor.White;

                    int r = 2;
                    foreach (var item in items)
                    {
                        ws.Cell(r, 1).Value = item.ItemName;
                        ws.Cell(r, 2).Value = item.CategoryName;
                        ws.Cell(r, 3).Value = item.QuantitySold;
                        ws.Cell(r, 4).Value = (double)item.UnitPrice;
                        ws.Cell(r, 5).Value = (double)item.CostPrice;
                        ws.Cell(r, 6).Value = (double)item.TotalRevenue;
                        ws.Cell(r, 7).Value = (double)item.TotalCogs;
                        ws.Cell(r, 8).Value = (double)item.Profit;
                        ws.Cell(r, 9).Value = item.MarginPercentage / 100.0;
                        ws.Cell(r, 9).Style.NumberFormat.Format = "0.0%";
                        ws.Cell(r, 10).Value = item.Recommendation;
                        r++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                    _notificationService.ShowSuccess("BI Margin report exported successfully.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Export failed: {ex.Message}");
                }
            }
        }
    }
}
