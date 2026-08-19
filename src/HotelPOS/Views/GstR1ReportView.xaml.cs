using ClosedXML.Excel;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace HotelPOS.Views
{
    public partial class GstR1ReportView : UserControl
    {
        private readonly INotificationService _notificationService;
        private readonly ObservableCollection<GstR1RowDto> _items = new();
        private List<GstR1RowDto> _allRows = new();
        private List<GstR1B2cSummaryDto> _b2cSummary = new();
        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _isLoading;

        public GstR1ReportView(INotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;

            if (System.Windows.Application.Current == null)
            {
                App.RegisterTestService(notificationService);
            }

            ReportGrid.ItemsSource = _items;

            Loaded += async (s, e) =>
            {
                FilterFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                FilterTo.SelectedDate = DateTime.Today;
                await LoadDataAsync();
            };
        }

        private async void Filter_Changed(object sender, EventArgs e)
        {
            if (!IsLoaded || _isLoading) return;
            await LoadDataAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadDataAsync();

        /// <summary>
        /// Loads orders in the selected date range and splits them into the B2B invoice-wise grid
        /// (customer has a GSTIN on file) and the B2C(Small) rate-wise summary (no GSTIN), matching
        /// the corresponding GSTR-1 filing tables.
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                var from = FilterFrom.SelectedDate;
                var to = FilterTo.SelectedDate?.AddDays(1);

                List<Order> allOrders;
                using (var scope = App.CreateDbScope())
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    allOrders = await orderService.GetAllOrdersWithItemsAsync();
                }

                var dateFiltered = allOrders.Where(o => !o.IsDeleted);
                if (from != null)
                    dateFiltered = dateFiltered.Where(o => o.CreatedAt.ToLocalTime() >= from.Value);
                if (to != null)
                    dateFiltered = dateFiltered.Where(o => o.CreatedAt.ToLocalTime() < to.Value);
                var dateFilteredList = dateFiltered.ToList();

                // B2B: the customer must have a GSTIN on file (GSTR-1 tables 4A/4B/4C/6B/6C -
                // invoice-wise details of supplies to registered persons).
                var b2bOrders = dateFilteredList.Where(o => !string.IsNullOrWhiteSpace(o.CustomerGstin));

                // One row per (invoice, tax rate) - an invoice with items at multiple GST rates
                // produces multiple rows, exactly as the official GSTR-1 B2B format requires.
                var rows = GstR1RowBuilder.BuildRows(b2bOrders)
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.InvoiceNumber)
                    .ToList();

                for (int i = 0; i < rows.Count; i++)
                    rows[i].SNo = i + 1;

                _allRows = rows;
                _items.Clear();
                _currentPage = 1;
                LoadMore();

                UpdateSummary(rows);

                // B2C(Small): walk-in/retail sales without a customer GSTIN, summarized by tax rate
                // (GSTR-1 table 7) rather than shown invoice-wise.
                var b2cOrders = dateFilteredList.Where(o => string.IsNullOrWhiteSpace(o.CustomerGstin));
                _b2cSummary = GstR1RowBuilder.BuildB2cSummary(b2cOrders);
                B2cSummaryGrid.ItemsSource = _b2cSummary;
                UpdateB2cSummary(_b2cSummary);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to load GSTR-1 report: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateSummary(List<GstR1RowDto> rows)
        {
            var distinctInvoices = rows
                .GroupBy(r => r.InvoiceNumber)
                .Select(g => g.First())
                .ToList();

            RecipientsBadge.Text = rows.Select(r => r.Gstin).Distinct().Count().ToString();
            InvoicesBadge.Text = distinctInvoices.Count.ToString();
            TaxableValueBadge.Text = $"Rs. {rows.Sum(r => r.TaxableValue):N2}";
            TaxAmountBadge.Text = $"Rs. {rows.Sum(r => r.Cgst + r.Sgst + r.Igst):N2}";
            TotalValueBadge.Text = $"Rs. {distinctInvoices.Sum(r => r.InvoiceValue):N2}";
        }

        private void UpdateB2cSummary(List<GstR1B2cSummaryDto> summary)
        {
            B2cInvoicesBadge.Text = summary.Sum(s => s.InvoiceCount).ToString();
            B2cTaxableValueBadge.Text = $"Rs. {summary.Sum(s => s.TaxableValue):N2}";
            B2cTaxAmountBadge.Text = $"Rs. {summary.Sum(s => s.TotalTax):N2}";
            B2cTotalValueBadge.Text = $"Rs. {summary.Sum(s => s.TotalValue):N2}";
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

        private void ReportGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.OriginalSource is not ScrollViewer sv) return;

            if (sv.VerticalOffset + sv.ViewportHeight >= sv.ExtentHeight - 50)
            {
                LoadMore();
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_allRows.Count == 0 && _b2cSummary.Count == 0)
            {
                _notificationService.ShowWarning("No data to export.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"GSTR1_Report_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (!dlg.ShowDialog().GetValueOrDefault()) return;

            try
            {
                using var wb = new XLWorkbook();

                var ws = wb.Worksheets.Add("B2B Invoices");

                string[] headers =
                {
                    "S.No", "GSTIN", "Invoice Number", "Date", "Invoice Value", "POS",
                    "Reverse Charge", "Invoice Type", "Customer", "Taxable Value",
                    "Item Total", "Rate", "CGST", "SGST", "IGST"
                };
                for (int c = 0; c < headers.Length; c++)
                    ws.Cell(1, c + 1).Value = headers[c];

                var headerRow = ws.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                headerRow.Style.Font.FontColor = XLColor.White;

                int row = 2;
                foreach (var r in _allRows)
                {
                    ws.Cell(row, 1).Value = r.SNo;
                    ws.Cell(row, 2).Value = r.Gstin.ForSpreadsheet();
                    ws.Cell(row, 3).Value = r.InvoiceNumber.ForSpreadsheet();
                    ws.Cell(row, 4).Value = r.Date.ToString("dd-MM-yyyy");
                    ws.Cell(row, 5).Value = (double)r.InvoiceValue;
                    ws.Cell(row, 6).Value = r.Pos;
                    ws.Cell(row, 7).Value = r.ReverseCharge;
                    ws.Cell(row, 8).Value = r.InvoiceType;
                    ws.Cell(row, 9).Value = r.CustomerName.ForSpreadsheet();
                    ws.Cell(row, 10).Value = (double)r.TaxableValue;
                    ws.Cell(row, 11).Value = (double)r.ItemTotal;
                    ws.Cell(row, 12).Value = (double)r.Rate;
                    ws.Cell(row, 13).Value = (double)r.Cgst;
                    ws.Cell(row, 14).Value = (double)r.Sgst;
                    ws.Cell(row, 15).Value = (double)r.Igst;
                    row++;
                }

                ws.Columns().AdjustToContents();

                var wsB2c = wb.Worksheets.Add("B2C Summary");
                string[] b2cHeaders =
                {
                    "Rate", "No. of Invoices", "Taxable Value", "CGST", "SGST", "IGST",
                    "Total Tax", "Total Value"
                };
                for (int c = 0; c < b2cHeaders.Length; c++)
                    wsB2c.Cell(1, c + 1).Value = b2cHeaders[c];

                var b2cHeaderRow = wsB2c.Row(1);
                b2cHeaderRow.Style.Font.Bold = true;
                b2cHeaderRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#173F5F");
                b2cHeaderRow.Style.Font.FontColor = XLColor.White;

                int b2cRow = 2;
                foreach (var s in _b2cSummary)
                {
                    wsB2c.Cell(b2cRow, 1).Value = (double)s.Rate;
                    wsB2c.Cell(b2cRow, 2).Value = s.InvoiceCount;
                    wsB2c.Cell(b2cRow, 3).Value = (double)s.TaxableValue;
                    wsB2c.Cell(b2cRow, 4).Value = (double)s.Cgst;
                    wsB2c.Cell(b2cRow, 5).Value = (double)s.Sgst;
                    wsB2c.Cell(b2cRow, 6).Value = (double)s.Igst;
                    wsB2c.Cell(b2cRow, 7).Value = (double)s.TotalTax;
                    wsB2c.Cell(b2cRow, 8).Value = (double)s.TotalValue;
                    b2cRow++;
                }

                wsB2c.Columns().AdjustToContents();

                wb.SaveAs(dlg.FileName);
                _notificationService.ShowSuccess("GSTR-1 report exported successfully.");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Export failed: {ex.Message}");
            }
        }
    }

    /// <summary>Builds GSTR-1 B2B report rows. Extracted as a standalone public class so the tax
    /// arithmetic and GSTIN parsing can be unit tested directly, without needing a running WPF
    /// UserControl or database.</summary>
    public static class GstR1RowBuilder
    {
        /// <summary>Expands each order into one row per distinct tax rate present on it, matching
        /// the GSTR-1 invoice-wise filing format (one line per invoice/rate combination). Shared by
        /// the B2B invoice-wise view and the B2C(Small) summary, which aggregates these same rows
        /// by rate so its figures always agree with what each invoice actually charged.</summary>
        public static List<GstR1RowDto> BuildRows(IEnumerable<Order> orders) =>
            orders
                .SelectMany(o => o.Items
                    .GroupBy(i => i.TaxPercentage)
                    .Select(rateGroup => BuildRow(o, rateGroup.Key, rateGroup.ToList())))
                .ToList();

        /// <summary>Aggregates B2C (no customer GSTIN) invoices into one row per tax rate, matching
        /// GSTR-1 table 7 (B2C Small). Built from the same per-invoice/rate rows as the B2B view so
        /// the taxable value/CGST/SGST here always reconcile with individual receipts.</summary>
        public static List<GstR1B2cSummaryDto> BuildB2cSummary(IEnumerable<Order> orders) =>
            BuildRows(orders)
                .GroupBy(r => r.Rate)
                .Select(g => new GstR1B2cSummaryDto
                {
                    Rate = g.Key,
                    InvoiceCount = g.Count(),
                    TaxableValue = g.Sum(r => r.TaxableValue),
                    Cgst = g.Sum(r => r.Cgst),
                    Sgst = g.Sum(r => r.Sgst),
                    Igst = g.Sum(r => r.Igst)
                })
                .OrderBy(s => s.Rate)
                .ToList();

        /// <summary>Computes the GST tax amount for a taxable value at a given rate, split evenly
        /// between CGST and SGST - rounding once, matching OrderService.CalculateTotals' own
        /// convention, so figures always agree with what was actually charged. IGST is never used
        /// since the app treats every sale as intrastate throughout the codebase. Shared by the
        /// GSTR-1 report and the Item Report so tax figures reconcile wherever they're shown.</summary>
        public static (decimal Cgst, decimal Sgst, decimal TaxAmount) ComputeTaxSplit(decimal taxableValue, decimal rate)
        {
            var taxAmount = Math.Round(taxableValue * (rate / 100m), 2);
            var cgst = Math.Round(taxAmount / 2m, 2);
            var sgst = taxAmount - cgst;
            return (cgst, sgst, taxAmount);
        }

        public static GstR1RowDto BuildRow(Order order, decimal rate, List<OrderItem> items)
        {
            var taxableValue = items.Sum(x => x.Total);
            var (cgst, sgst, taxAmount) = ComputeTaxSplit(taxableValue, rate);

            return new GstR1RowDto
            {
                Gstin = order.CustomerGstin ?? string.Empty,
                InvoiceNumber = order.InvoiceNumber ?? string.Empty,
                Date = order.CreatedAt.ToLocalTime(),
                InvoiceValue = order.TotalAmount,
                Pos = DerivePlaceOfSupply(order.CustomerGstin),
                ReverseCharge = "N",
                InvoiceType = "R",
                CustomerName = order.CustomerName ?? string.Empty,
                TaxableValue = taxableValue,
                ItemTotal = taxableValue + taxAmount,
                Rate = rate,
                Cgst = cgst,
                Sgst = sgst,
                // Always intrastate: the app has no concept of the hotel's own operating state vs
                // the customer's, so every sale is treated as intrastate (CGST+SGST) throughout the
                // codebase (see OrderService.CalculateTotals) - IGST is never actually charged, so
                // showing it here would disagree with what the receipt/order totals record.
                Igst = 0m
            };
        }

        /// <summary>The first two digits of a GSTIN are the issuing state's GST state code, which is
        /// what GSTR-1 filing tools show as "Place of Supply" for a B2B row.</summary>
        public static string DerivePlaceOfSupply(string? gstin) =>
            !string.IsNullOrWhiteSpace(gstin) && gstin.Length >= 2 ? gstin[..2] : string.Empty;
    }

    /// <summary>One row = one tax rate present on one B2B invoice, matching the official GSTR-1
    /// B2B (4A/4B/4C/6B/6C) invoice-wise filing format.</summary>
    public class GstR1RowDto
    {
        public int SNo { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal InvoiceValue { get; set; }
        public string Pos { get; set; } = string.Empty;
        public string ReverseCharge { get; set; } = "N";
        public string InvoiceType { get; set; } = "R";
        public string CustomerName { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal Rate { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
    }

    /// <summary>One row per tax rate, aggregating all B2C (no customer GSTIN) invoices in the
    /// period - matches the GSTR-1 table 7 (B2C Small) summary format.</summary>
    public class GstR1B2cSummaryDto
    {
        public decimal Rate { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal TotalTax => Cgst + Sgst + Igst;
        public decimal TotalValue => TaxableValue + TotalTax;
    }
}
