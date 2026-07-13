using Microsoft.Extensions.DependencyInjection;
using HotelPOS.Application.Interfaces;
using ClosedXML.Excel;
using HotelPOS.ViewModels;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class PurchaseReportView : UserControl
    {
        private readonly PurchaseReportViewModel _viewModel;

        public PurchaseReportView(PurchaseReportViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Pager removed.
            // _viewModel.GetPageSizeRequested and _viewModel.SetPagerTotalCount are removed from ViewModel.

            Loaded += async (s, e) =>
            {
                await _viewModel.InitializeAsync();
            };
        }

        private void ReportGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var sv = e.OriginalSource as ScrollViewer;
            if (sv == null) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                _ = _viewModel.LoadMoreAsync();
            }
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var items = _viewModel.ReportRows;
            if (items == null || !items.Any())
            {
                App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage("No data to export.", "Warning", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Purchase_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    using var wb = new XLWorkbook();
                    var ws = wb.Worksheets.Add("Purchase Report");

                    // Headers
                    ws.Cell(1, 1).Value = "Date";
                    ws.Cell(1, 2).Value = "Invoice No";
                    ws.Cell(1, 3).Value = "Supplier";
                    ws.Cell(1, 4).Value = "Item Name";
                    ws.Cell(1, 5).Value = "Qty";
                    ws.Cell(1, 6).Value = "Price";
                    ws.Cell(1, 7).Value = "Tax Amount";
                    ws.Cell(1, 8).Value = "Discount";
                    ws.Cell(1, 9).Value = "Total Amount";
                    ws.Cell(1, 10).Value = "Payment";

                    var headerRow = ws.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                    headerRow.Style.Font.FontColor = XLColor.White;

                    int row = 2;
                    foreach (var item in items)
                    {
                        ws.Cell(row, 1).Value = item.PurchaseDate.ToString("g");
                        ws.Cell(row, 2).Value = item.InvoiceNumber;
                        ws.Cell(row, 3).Value = item.SupplierName;
                        ws.Cell(row, 4).Value = item.ItemName;
                        ws.Cell(row, 5).Value = item.Quantity;
                        ws.Cell(row, 6).Value = (double)item.UnitPrice;
                        ws.Cell(row, 7).Value = (double)item.TaxAmount;
                        ws.Cell(row, 8).Value = (double)item.Discount;
                        ws.Cell(row, 9).Value = (double)item.TotalAmount;
                        ws.Cell(row, 10).Value = item.PaymentType;
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    wb.SaveAs(dlg.FileName);
                    App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage("Purchase report exported successfully.", "Success", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Information);
                }
                catch (Exception ex)
                {
                    App.CurrentApp!.ServiceProvider.GetRequiredService<HotelPOS.Application.Interfaces.IDialogService>().ShowMessage($"Export failed: {ex.Message}", "Error", HotelPOS.Application.Interfaces.DialogButton.OK, HotelPOS.Application.Interfaces.DialogIcon.Error);
                }
            }
        }
    }
}
