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

            // Wire up pagination control
            Pager.ExternalPageRequested += async (page, pageSize) => await _viewModel.LoadPageAsync(page, pageSize);
            _viewModel.GetPageSizeRequested = () =>
            {
                // Accessing Pager control's pageSize (combobox) logic
                // Since PageSize is internal to Pager, we can just use 10 as default or read it
                // Actually, the Pager passes pageSize in ExternalPageRequested, so we just use that.
                // Let's assume default 10 for Apply Filters
                return 10;
            };

            _viewModel.SetPagerTotalCount = (total) => Pager.SetExternalSource(total);

            Loaded += async (s, e) =>
            {
                await _viewModel.InitializeAsync();
            };
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var items = _viewModel.ReportRows;
            if (items == null || !items.Any())
            {
                MessageBox.Show("No data to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Purchase_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog() == true)
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
                    MessageBox.Show("Purchase report exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
