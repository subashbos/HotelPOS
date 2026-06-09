using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;

namespace HotelPOS.Views
{
    public partial class ItemReportView : UserControl
    {
        private readonly IOrderService _orderService;
        private readonly ICategoryService _categoryService;
        private readonly IItemService _itemService;
        private readonly INotificationService _notificationService;
        private bool _isLoading;

        public ItemReportView(IOrderService orderService, ICategoryService categoryService, IItemService itemService, INotificationService notificationService)
        {
            InitializeComponent();
            _orderService = orderService;
            _categoryService = categoryService;
            _itemService = itemService;
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(orderService);
                App.RegisterTestService(categoryService);
                App.RegisterTestService(itemService);
                App.RegisterTestService(notificationService);
            }

            Pager.PageChanged += page =>
            {
                ReportGrid.ItemsSource = page;
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

                // 3. Extract and group order items
                var orderItems = filteredOrders.SelectMany(o => o.Items);

                var grouped = orderItems
                    .GroupBy(i => i.ItemId)
                    .Select(g =>
                    {
                        var catalogItem = allItems.FirstOrDefault(x => x.Id == g.Key);
                        return new ItemSalesReportRowDto
                        {
                            ItemId = g.Key,
                            ItemName = catalogItem?.Name ?? g.First().ItemName,
                            CategoryId = catalogItem?.CategoryId ?? 0,
                            CategoryName = catalogItem?.Category?.Name ?? "General",
                            Price = catalogItem?.Price ?? g.First().Price,
                            QuantitySold = g.Sum(x => x.Quantity),
                            TotalRevenue = g.Sum(x => x.Total)
                        };
                    })
                    .ToList();

                // 4. Apply category filter
                if (categoryId != null && categoryId > 0)
                {
                    grouped = grouped.Where(x => x.CategoryId == categoryId).ToList();
                }

                // 5. Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    grouped = grouped.Where(x => x.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 6. Sort by total revenue descending
                grouped = grouped.OrderByDescending(x => x.TotalRevenue).ToList();

                // 7. Assign SNo
                for (int i = 0; i < grouped.Count; i++)
                {
                    grouped[i].SNo = i + 1;
                }

                // 8. Bind and update totals
                Pager.SetSource(grouped);
                TotalQtySold.Text = grouped.Sum(x => x.QuantitySold).ToString();
                TotalRevenueSum.Text = $"Rs. {grouped.Sum(x => x.TotalRevenue):N2}";
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

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var items = ReportGrid.ItemsSource as IEnumerable<ItemSalesReportRowDto>;
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

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Item Report");

                    // Headers
                    ws.Cell(1, 1).Value = "S.No";
                    ws.Cell(1, 2).Value = "Item Name";
                    ws.Cell(1, 3).Value = "Category";
                    ws.Cell(1, 4).Value = "Current Price";
                    ws.Cell(1, 5).Value = "Quantity Sold";
                    ws.Cell(1, 6).Value = "Total Revenue";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                    headerRow.Style.Font.FontColor = XLColor.White;

                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cell(row, 1).Value = item.SNo;
                        ws.Cell(row, 2).Value = item.ItemName;
                        ws.Cell(row, 3).Value = item.CategoryName;
                        ws.Cell(row, 4).Value = (double)item.Price;
                        ws.Cell(row, 5).Value = item.QuantitySold;
                        ws.Cell(row, 6).Value = (double)item.TotalRevenue;
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

    public class ItemSalesReportRowDto
    {
        public int SNo { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
