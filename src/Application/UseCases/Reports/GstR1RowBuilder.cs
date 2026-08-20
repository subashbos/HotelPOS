using HotelPOS.Application.DTOs.Report;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Application.UseCases.Reports
{
    /// <summary>Builds GSTR-1 B2B/B2C/HSN report rows. Pure over domain entities so the tax
    /// arithmetic and GSTIN parsing can be unit tested directly, without a database.</summary>
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
            // Computed fresh as Price * Quantity rather than trusted from the stored Total field:
            // orders placed before the 2026-07-23 security fix (repricing lines from the item
            // catalog) had Total written as a tax-INCLUSIVE figure by the client, so summing Total
            // directly would double-count tax on every pre-fix invoice.
            var taxableValue = items.Sum(x => x.Price * x.Quantity);
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

        /// <summary>Aggregates ALL outward supplies in the period (B2B and B2C combined, unlike the
        /// other tabs) into one row per HSN code and tax rate, matching GSTR-1 table 12 (HSN-wise
        /// summary). Items whose catalog entry has no HSN code set are grouped under "(No HSN)" so
        /// the gap stays visible in the report rather than being silently dropped.</summary>
        public static List<HsnSummaryRowDto> BuildHsnSummary(IEnumerable<Order> orders, IReadOnlyDictionary<int, Item> catalogById)
        {
            return orders
                .SelectMany(o => o.Items)
                .GroupBy(oi =>
                {
                    catalogById.TryGetValue(oi.ItemId, out var catalogItem);
                    var hsn = string.IsNullOrWhiteSpace(catalogItem?.HsnCode) ? "(No HSN)" : catalogItem!.HsnCode!;
                    return (Hsn: hsn, oi.TaxPercentage);
                })
                .Select(g =>
                {
                    var taxableValue = g.Sum(oi => oi.Price * oi.Quantity);
                    var (cgst, sgst, _) = ComputeTaxSplit(taxableValue, g.Key.TaxPercentage);
                    var representative = g.First();
                    catalogById.TryGetValue(representative.ItemId, out var catalogItem);
                    return new HsnSummaryRowDto
                    {
                        HsnCode = g.Key.Hsn,
                        Description = catalogItem?.Name ?? representative.ItemName,
                        Uqc = catalogItem?.Unit?.Name ?? string.Empty,
                        TotalQuantity = g.Sum(oi => oi.Quantity),
                        TaxableValue = taxableValue,
                        Rate = g.Key.TaxPercentage,
                        Cgst = cgst,
                        Sgst = sgst,
                        // Always intrastate, same rationale as BuildRow above.
                        Igst = 0m
                    };
                })
                .OrderBy(r => r.HsnCode)
                .ThenBy(r => r.Rate)
                .ToList();
        }
    }
}
