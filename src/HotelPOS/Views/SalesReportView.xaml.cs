using ClosedXML.Excel;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class SalesReportView : UserControl
    {
        private readonly INotificationService _notificationService;
        private readonly ObservableCollection<RecentOrderRowDto> _items = new();
        private List<RecentOrderRowDto> _allRows = new();
        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _isLoading;

        public SalesReportView(INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(notificationService);
            }

            SalesGrid.ItemsSource = _items;

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
        /// Loads categories from the category service, sorts them by display order then name, inserts an "All Categories" entry at the top, and populates the category combo box selecting the first item.
        /// </summary>
        /// <remarks>Exceptions thrown while loading categories are caught and ignored.</remarks>
        private async Task LoadCategoriesAsync()
        {
            try
            {
                IEnumerable<HotelPOS.Domain.Entities.Category> cats;
                using (var scope = App.CreateDbScope())
                {
                    var categoryService = scope.ServiceProvider.GetRequiredService<ICategoryService>();
                    cats = await categoryService.GetCategoriesAsync();
                }

                var list = cats.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToList();
                list.Insert(0, new HotelPOS.Domain.Entities.Category { Id = 0, Name = "All Categories", DisplayOrder = -1 });
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

        /// <summary>
        /// Loads sales report data using the current filter controls and updates the grid, pager, and summary totals in the UI.
        /// </summary>
        /// <remarks>
        /// Prevents concurrent loads while running. Reads date range, search text, payment mode, selected category, and order type from the view controls,
        /// retrieves matching orders from IOrderService within a scoped DI scope, maps them to RecentOrderRowDto entries, sets the Pager source,
        /// and updates the total orders count and total revenue display. Any errors are reported via the notification service.
        /// </remarks>
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
                if (TypeDine.IsChecked is true) orderType = OrderTypes.DineIn;
                else if (TypeTake.IsChecked is true) orderType = OrderTypes.Takeaway;
                else if (TypeOnline.IsChecked is true) orderType = OrderTypes.Online;

                // We use a large page size for the report or handle pagination
                (IEnumerable<HotelPOS.Domain.Entities.Order> orders, int totalCount) result;
                using (var scope = App.CreateDbScope())
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    result = await orderService.GetPagedOrdersAsync(new PagedOrdersRequest(1, 1000, from, to, null, search, payment, orderType, categoryId));
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
                    Status = o.Status,
                    CustomerName = o.CustomerName,
                    CustomerPhone = o.CustomerPhone,
                    CustomerGstin = o.CustomerGstin,
                    Items = o.Items
                }).ToList();

                _allRows = reportRows;
                _items.Clear();
                _currentPage = 1;
                LoadMore();

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

        private void LoadMore()
        {
            var itemsToLoad = _allRows.Skip((_currentPage - 1) * PageSize).Take(PageSize).ToList();
            if (itemsToLoad.Any())
            {
                foreach (var i in itemsToLoad) _items.Add(i);
                _currentPage++;
            }
        }

        private void SalesGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                LoadMore();
            }
        }

        private void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                var window = Window.GetWindow(this) as DashboardWindow;
                if (window != null)
                {
                    var order = new HotelPOS.Domain.Entities.Order
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

        /// <summary>
        /// Opens a print preview window for the order represented by the clicked button's Tag.
        /// </summary>
        /// <remarks>
        /// If the sender is not a Button or its Tag is not a RecentOrderRowDto, no action is taken. The method loads system settings before showing the preview and shows an error notification if the preview cannot be opened.
        /// </remarks>
        private async void PrintOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                try
                {
                    HotelPOS.Domain.Entities.SystemSetting settings;
                    using (var scope = App.CreateDbScope())
                    {
                        var settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
                        settings = await settingService.GetSettingsAsync();
                    }

                    var order = new HotelPOS.Domain.Entities.Order
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

        private async void VoidOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                var confirm = App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage($"Are you sure you want to void order invoice {row.InvoiceNumber}?", "Confirm Void", HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Warning);
                if (confirm == HotelPOS.Application.Interfaces.DialogResult.Yes)
                {
                    string reason = Microsoft.VisualBasic.Interaction.InputBox("Enter reason for voiding this order:", "Void Reason", "Customer Cancellation");
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        _notificationService.ShowWarning("Void cancelled: Reason is required.");
                        return;
                    }

                    try
                    {
                        using (var scope = App.CreateDbScope())
                        {
                            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                            await orderService.VoidOrderAsync(row.OrderId, reason, RoleNames.Manager);
                        }
                        _notificationService.ShowSuccess($"Order {row.InvoiceNumber} voided successfully.");
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"Failed to void order: {ex.Message}");
                    }
                }
            }
        }

        private async void RefundOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is RecentOrderRowDto row)
            {
                var confirm = App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage($"Are you sure you want to refund order invoice {row.InvoiceNumber}?", "Confirm Refund", HotelPOS.Application.Interfaces.DialogButton.YesNo, HotelPOS.Application.Interfaces.DialogIcon.Question);
                if (confirm == HotelPOS.Application.Interfaces.DialogResult.Yes)
                {
                    string reason = Microsoft.VisualBasic.Interaction.InputBox("Enter reason for refunding this order:", "Refund Reason", "Return/Refund");
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        _notificationService.ShowWarning("Refund cancelled: Reason is required.");
                        return;
                    }

                    try
                    {
                        using (var scope = App.CreateDbScope())
                        {
                            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                            var fullRefundItems = row.Items.Select(i => new OrderItemRefundDto(i.ItemId, i.Quantity)).ToList();
                            await orderService.RefundOrderAsync(row.OrderId, fullRefundItems, reason);
                        }
                        _notificationService.ShowSuccess($"Order {row.InvoiceNumber} refunded successfully.");
                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        _notificationService.ShowError($"Failed to refund order: {ex.Message}");
                    }
                }
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
                FileName = $"Sales_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog() is true)
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
