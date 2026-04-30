using HotelPOS.Domain;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace HotelPOS
{
    public static class ReceiptGenerator
    {
        public static FlowDocument CreateReceipt(Order order, bool isThermal, SystemSetting settings)
        {
            var doc = new FlowDocument
            {
                PagePadding = isThermal ? new Thickness(10, 10, 10, 20) : new Thickness(50),
                ColumnWidth = isThermal ? 280 : 700,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Left
            };

            if (isThermal) doc.MaxPageWidth = 300;

            int titleSz = isThermal ? 18 : 26;
            int headSz = isThermal ? 14 : 18;
            int textSz = isThermal ? 12 : 14;
            int smallSz = isThermal ? 10 : 12;

            // ── Hotel Header ──────────────────────────────────────────────────
            var hdr = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 6) };
            hdr.Inlines.Add(new Run(settings.HotelName.ToUpper() + "\n") { FontSize = titleSz, FontWeight = FontWeights.Bold });
            hdr.Inlines.Add(new Run(settings.HotelAddress + "\n") { FontSize = smallSz });
            if (settings.ShowPhoneOnReceipt && !string.IsNullOrWhiteSpace(settings.HotelPhone))
                hdr.Inlines.Add(new Run($"Ph: {settings.HotelPhone}\n") { FontSize = smallSz });
            if (!string.IsNullOrWhiteSpace(settings.HotelGst))
                hdr.Inlines.Add(new Run($"GSTIN: {settings.HotelGst}\n") { FontSize = smallSz, FontWeight = FontWeights.SemiBold });

            hdr.Inlines.Add(new Run(new string('-', isThermal ? 38 : 86) + "\n") { FontSize = smallSz });
            hdr.Inlines.Add(new Run($"Date  : {order.CreatedAt.ToLocalTime():dd-MMM-yyyy  hh:mm tt}\n") { FontSize = smallSz });
            hdr.Inlines.Add(new Run($"Receipt #: {order.Id}  |  Table: {order.TableNumber}\n") { FontSize = smallSz });

            // B2B Customer Details
            bool hasCustomer = !string.IsNullOrWhiteSpace(order.CustomerName) || 
                               !string.IsNullOrWhiteSpace(order.CustomerPhone) || 
                               !string.IsNullOrWhiteSpace(order.CustomerGstin);
            if (hasCustomer)
            {
                hdr.Inlines.Add(new Run(new string('-', isThermal ? 38 : 86) + "\n") { FontSize = smallSz });
                hdr.Inlines.Add(new Run("BILLED TO:\n") { FontSize = smallSz, FontWeight = FontWeights.Bold });
                if (!string.IsNullOrWhiteSpace(order.CustomerName))
                    hdr.Inlines.Add(new Run($"{order.CustomerName.ToUpper()}\n") { FontSize = smallSz });
                if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
                    hdr.Inlines.Add(new Run($"Ph: {order.CustomerPhone}\n") { FontSize = smallSz });
                if (!string.IsNullOrWhiteSpace(order.CustomerGstin))
                    hdr.Inlines.Add(new Run($"GSTIN: {order.CustomerGstin}\n") { FontSize = smallSz, FontWeight = FontWeights.SemiBold });
            }

            doc.Blocks.Add(hdr);

            // Separator
            doc.Blocks.Add(new Paragraph(new Run(new string(isThermal ? '-' : '_', isThermal ? 38 : 86)))
            { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });

            // ── Items Table ───────────────────────────────────────────────────
            var table = new Table { Margin = new Thickness(0, 4, 0, 4) };

            if (isThermal)
            {
                // S.No | Item Name | GST% | Tax | Qty | Total
                table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) }); // S.No
                table.Columns.Add(new TableColumn { Width = new GridLength(3.6, GridUnitType.Star) }); // Name
                table.Columns.Add(new TableColumn { Width = new GridLength(0.6, GridUnitType.Star) }); // Qty
                table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) }); // Total
            }
            else
            {
                // S.No | Item Name | Unit Price | GST% | Tax | Qty | Total
                table.Columns.Add(new TableColumn { Width = new GridLength(0.5, GridUnitType.Star) }); // S.No
                table.Columns.Add(new TableColumn { Width = new GridLength(4.8, GridUnitType.Star) }); // Name
                table.Columns.Add(new TableColumn { Width = new GridLength(1.4, GridUnitType.Star) }); // Unit Price
                table.Columns.Add(new TableColumn { Width = new GridLength(0.8, GridUnitType.Star) }); // Qty
                table.Columns.Add(new TableColumn { Width = new GridLength(1.4, GridUnitType.Star) }); // Total
            }

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            // Header row — bold, slightly shaded
            var headerRow = new TableRow { FontWeight = FontWeights.Bold, FontSize = textSz };
            headerRow.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            headerRow.Cells.Add(MakeCell("#", TextAlignment.Left));
            headerRow.Cells.Add(MakeCell("ITEM", TextAlignment.Left));
            if (!isThermal) headerRow.Cells.Add(MakeCell("PRICE", TextAlignment.Right));
            headerRow.Cells.Add(MakeCell("QTY", TextAlignment.Center));
            headerRow.Cells.Add(MakeCell("TOTAL", TextAlignment.Right));
            rowGroup.Rows.Add(headerRow);

            var items = order.Items ?? new List<OrderItem>();
            int sNo = 1;
            foreach (var item in items)
            {
                var tr = new TableRow { FontSize = textSz };
                tr.Cells.Add(MakeCell(sNo.ToString(), TextAlignment.Left));
                tr.Cells.Add(MakeCell(item.ItemName, TextAlignment.Left));
                if (!isThermal) tr.Cells.Add(MakeCell(item.Price.ToString("N2"), TextAlignment.Right));
                tr.Cells.Add(MakeCell(item.Quantity.ToString(), TextAlignment.Center));
                tr.Cells.Add(MakeCell(item.Total.ToString("N2"), TextAlignment.Right));
                rowGroup.Rows.Add(tr);
                sNo++;
            }

            doc.Blocks.Add(table);

            // Separator
            doc.Blocks.Add(new Paragraph(new Run(new string(isThermal ? '-' : '_', isThermal ? 38 : 86)))
            { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 4) });

            // ── Totals Table ──────────────────────────────────────────────────
            decimal subtotal = order.Subtotal;
            decimal gstTotal = order.GstAmount;
            decimal grandTotal = order.TotalAmount;

            var totals = new Table { Margin = new Thickness(0, 6, 0, 0) };
            totals.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            totals.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var tg = new TableRowGroup();
            totals.RowGroups.Add(tg);

            if (settings.ShowGstBreakdown)
            {
                AddTotalsRow(tg, "Subtotal:", subtotal.ToString("N2"), false, textSz);
                
                var taxGroups = items
                    .GroupBy(i => i.TaxPercentage)
                    .OrderBy(g => g.Key);

                foreach (var group in taxGroups)
                {
                    if (group.Key == 0) continue;
                    
                    decimal groupSubtotal = group.Sum(i => i.Price * i.Quantity);
                    decimal groupTax = groupSubtotal * (group.Key / 100m);
                    decimal halfTax = groupTax / 2;
                    decimal halfRate = group.Key / 2;

                    AddTotalsRow(tg, $"CGST ({halfRate:0.#}%):", halfTax.ToString("N2"), false, textSz);
                    AddTotalsRow(tg, $"SGST ({halfRate:0.#}%):", halfTax.ToString("N2"), false, textSz);
                }
            }
            if (order.DiscountAmount > 0)
                AddTotalsRow(tg, "Discount:", "-" + order.DiscountAmount.ToString("N2"), false, textSz);

            if (settings.EnableRoundOff)
            {
                var rounded = Math.Round(grandTotal, 0, MidpointRounding.AwayFromZero);
                var diff = rounded - grandTotal;
                if (diff != 0)
                    AddTotalsRow(tg, "Round Off:", (diff >= 0 ? "+" : "") + diff.ToString("N2"), false, textSz);
                grandTotal = rounded;
            }
            AddTotalsRow(tg, "Grand Total:", grandTotal.ToString("N2"), true, headSz);
            AddTotalsRow(tg, "Payment:", order.PaymentMode, false, smallSz);
            doc.Blocks.Add(totals);

            // ── Footer ────────────────────────────────────────────────────────
            if (settings.ShowThankYouFooter)
            {
                var ftr = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 16, 0, 0) };
                ftr.Inlines.Add(new Run(new string(isThermal ? '-' : '_', isThermal ? 38 : 86) + "\n") { FontSize = smallSz });
                ftr.Inlines.Add(new Run("Thank you for dining with us!\n") { FontSize = textSz, FontStyle = FontStyles.Italic });
                ftr.Inlines.Add(new Run("Please visit again\n") { FontSize = smallSz });
                doc.Blocks.Add(ftr);
            }

            return doc;
        }

        private static TableCell MakeCell(string text, TextAlignment align)
            => new TableCell(new Paragraph(new Run(text))
            { TextAlignment = align, Margin = new Thickness(0, 3, 4, 3) });

        private static void AddTotalsRow(TableRowGroup group, string label, string value, bool bold, int size)
        {
            var row = new TableRow { FontSize = size };
            if (bold) row.FontWeight = FontWeights.Bold;
            row.Cells.Add(new TableCell(new Paragraph(new Run(label))
            { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 2, 8, 2) }));
            row.Cells.Add(new TableCell(new Paragraph(new Run(value))
            { TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 2, 0, 2) }));
            group.Rows.Add(row);
        }

        public static FlowDocument CreateKOT(int tableNumber, List<OrderItem> items, bool isThermal)
        {
            var doc = new FlowDocument
            {
                PagePadding = isThermal ? new Thickness(10, 10, 10, 20) : new Thickness(50),
                ColumnWidth = isThermal ? 280 : 700,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Left
            };

            if (isThermal) doc.MaxPageWidth = 300;

            int titleSz = isThermal ? 22 : 28;
            int textSz = isThermal ? 14 : 16;
            int smallSz = isThermal ? 12 : 14;

            // Header
            var hdr = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            hdr.Inlines.Add(new Run("K.O.T\n") { FontSize = titleSz, FontWeight = FontWeights.Bold });
            hdr.Inlines.Add(new Run($"(Kitchen Order Ticket)\n") { FontSize = smallSz });
            hdr.Inlines.Add(new Run(new string('-', isThermal ? 30 : 60) + "\n") { FontSize = smallSz });
            
            hdr.Inlines.Add(new Run($"Table : {tableNumber}\n") { FontSize = textSz, FontWeight = FontWeights.Bold });
            hdr.Inlines.Add(new Run($"Time  : {DateTime.Now:dd-MMM-yyyy hh:mm tt}\n") { FontSize = smallSz });
            hdr.Inlines.Add(new Run(new string('-', isThermal ? 30 : 60) + "\n") { FontSize = smallSz });
            doc.Blocks.Add(hdr);

            // Items Table
            var table = new Table { Margin = new Thickness(0, 4, 0, 4) };
            table.Columns.Add(new TableColumn { Width = new GridLength(4, GridUnitType.Star) }); // Name
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) }); // Qty

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerRow = new TableRow { FontWeight = FontWeights.Bold, FontSize = textSz };
            headerRow.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));
            headerRow.Cells.Add(MakeCell("ITEM", TextAlignment.Left));
            headerRow.Cells.Add(MakeCell("QTY", TextAlignment.Center));
            rowGroup.Rows.Add(headerRow);

            foreach (var item in items)
            {
                var tr = new TableRow { FontSize = textSz, FontWeight = FontWeights.SemiBold };
                tr.Cells.Add(MakeCell(item.ItemName, TextAlignment.Left));
                tr.Cells.Add(MakeCell(item.Quantity.ToString(), TextAlignment.Center));
                rowGroup.Rows.Add(tr);
            }

            doc.Blocks.Add(table);

            doc.Blocks.Add(new Paragraph(new Run(new string('-', isThermal ? 30 : 60)))
            { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });

            return doc;
        }
    }
}
