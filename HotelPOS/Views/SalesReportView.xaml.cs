using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application;
using HotelPOS.Application.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;

namespace HotelPOS.Views
{
    public partial class SalesReportView : UserControl
    {
        private readonly IOrderService _orderService;
        private readonly ICategoryService _categoryService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private bool _isLoading;

        public SalesReportView(IOrderService orderService, ICategoryService categoryService, ISettingService settingService, INotificationService notificationService)
        {
            InitializeComponent();
            _orderService = orderService;
            _categoryService = categoryService;
            _settingService = settingService;
            _notificationService = notificationService;

            Pager.PageChanged += page =>
            {
                SalesGrid.ItemsSource = page;
            };

            Loaded += async (s, e) => {
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                await App.DbLock.WaitAsync();
                IEnumerable<HotelPOS.Domain.Category> cats;
                try
                {
                    cats = await _categoryService.GetCategoriesAsync();
                }
                finally
                {
                    App.DbLock.Release();
                }
                
                var list = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
                list.Insert(0, new HotelPOS.Domain.Category { Id = 0, Name = "All Categories", DisplayOrder = -1 });
                ComboCategory.ItemsSource = list;
                ComboCategory.SelectedIndex = 0;
            }
            catch { }
        }

        private async void Filter_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded || _isLoading) return;
            await LoadDataAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        public async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var from = FilterFrom.SelectedDate;
                var to = FilterTo.SelectedDate?.AddDays(1);
                var search = SearchText.Text;
                var payment = (ComboPayment.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
                var categoryId = (int?)ComboCategory.SelectedValue;
                
                string orderType = "All";
                if (TypeDine.IsChecked == true) orderType = "DineIn";
                else if (TypeTake.IsChecked == true) orderType = "Takeaway";
                else if (TypeOnline.IsChecked == true) orderType = "Online";

                // We use a large page size for the report or handle pagination
                await App.DbLock.WaitAsync();
                (IEnumerable<HotelPOS.Domain.Order> orders, int totalCount) result;
                try
                {
                    result = await _orderService.GetPagedOrdersAsync(1, 1000, from, to, null, search, payment, orderType, categoryId);
                }
                finally
                {
                    App.DbLock.Release();
                }

                var reportRows = result.orders.Select((o, idx) => new RecentOrderRowDto
                {
                    SNo = idx + 1,
                    OrderId = o.Id,
                    InvoiceNumber = o.InvoiceNumber,
                    CreatedAt = o.CreatedAt.ToLocalTime(),
                    TableNumber = o.TableNumber,
                    Total = o.TotalAmount,
                    DiscountAmount = o.DiscountAmount,
                    ItemCount = o.Items.Count,
                    PaymentMode = o.PaymentMode,
                    OrderType = o.OrderType,
                    CustomerName = o.CustomerName,
                    CustomerPhone = o.CustomerPhone,
                    CustomerGstin = o.CustomerGstin,
                    Items = o.Items
                }).ToList();

                Pager.SetSource(reportRows);
                TotalOrdersCount.Text = result.totalCount.ToString();
                TotalRevenueSum.Text = $"Rs. {reportRows.Sum(x => x.Total):N2}";
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load sales report: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
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
                        InvoiceNumber = row.InvoiceNumber,
                        TableNumber = row.TableNumber,
                        CreatedAt = row.CreatedAt,
                        Items = row.Items,
                        TotalAmount = row.Total,
                        DiscountAmount = row.DiscountAmount,
                        PaymentMode = row.PaymentMode,
                        OrderType = row.OrderType,
                        CustomerName = row.CustomerName,
                        CustomerPhone = row.CustomerPhone,
                        CustomerGstin = row.CustomerGstin
                    };
                    window.StartEditOrder(order);
                }
            }
        }

        private async void PrintOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                try
                {
                    var settings = await _settingService.GetSettingsAsync();
                    var order = new HotelPOS.Domain.Order
                    {
                        Id = row.OrderId,
                        InvoiceNumber = row.InvoiceNumber,
                        TableNumber = row.TableNumber,
                        CreatedAt = row.CreatedAt,
                        Items = row.Items,
                        TotalAmount = row.Total,
                        DiscountAmount = row.DiscountAmount,
                        PaymentMode = row.PaymentMode,
                        OrderType = row.OrderType,
                        CustomerName = row.CustomerName,
                        CustomerPhone = row.CustomerPhone,
                        CustomerGstin = row.CustomerGstin
                    };

                    var preview = new PrintPreviewWindow(order, settings);
                    preview.Owner = Window.GetWindow(this);
                    preview.ShowDialog();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Could not open print preview: {ex.Message}");
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var items = SalesGrid.ItemsSource as IEnumerable<RecentOrderRowDto>;
            if (items == null || !items.Any())
            {
                _notificationService.ShowWarning("No data to export.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Sales Report");

                    // Headers
                    ws.Cell(1, 1).Value = "Date";
                    ws.Cell(1, 2).Value = "Invoice";
                    ws.Cell(1, 3).Value = "Customer";
                    ws.Cell(1, 4).Value = "Type";
                    ws.Cell(1, 5).Value = "Payment";
                    ws.Cell(1, 6).Value = "Total Amount";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                    headerRow.Style.Font.FontColor = XLColor.White;

                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cell(row, 1).Value = item.CreatedAt.ToString("g");
                        ws.Cell(row, 2).Value = item.InvoiceNumber;
                        ws.Cell(row, 3).Value = item.CustomerName ?? "N/A";
                        ws.Cell(row, 4).Value = item.OrderType;
                        ws.Cell(row, 5).Value = item.PaymentMode;
                        ws.Cell(row, 6).Value = (double)item.Total;
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                    _notificationService.ShowSuccess("Sales report exported successfully.");
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"Export failed: {ex.Message}");
                }
            }
        }
    }
}
