using HotelPOS.Application.DTOs.Report;
using HotelPOS.Domain.Common;
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
        /// <param name="hotelGstin">The business's own GSTIN (Settings). When its state code differs
        /// from an order's customer GSTIN, that row is taxed as IGST instead of CGST+SGST.</param>
        public static List<GstR1RowDto> BuildRows(IEnumerable<Order> orders, string? hotelGstin = null) =>
            orders
                .SelectMany(o => o.Items
                    .GroupBy(i => i.TaxPercentage)
                    .Select(rateGroup => BuildRow(o, rateGroup.Key, rateGroup.ToList(), hotelGstin)))
                .ToList();

        /// <summary>Aggregates B2C (no customer GSTIN) invoices into one row per tax rate, matching
        /// GSTR-1 table 7 (B2C Small). Built from the same per-invoice/rate rows as the B2B view so
        /// the taxable value/CGST/SGST here always reconcile with individual receipts.</summary>
        public static List<GstR1B2cSummaryDto> BuildB2cSummary(IEnumerable<Order> orders, string? hotelGstin = null) =>
            BuildRows(orders, hotelGstin)
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
        /// convention, so figures always agree with what was actually charged for an intrastate
        /// sale. Shared by the GSTR-1 report and the Item Report so tax figures reconcile wherever
        /// they're shown. Callers that need to know whether IGST applies instead should check
        /// <see cref="GstinHelper.IsInterstate"/> before choosing whether to use this split.</summary>
        public static (decimal Cgst, decimal Sgst, decimal TaxAmount) ComputeTaxSplit(decimal taxableValue, decimal rate)
        {
            var taxAmount = Math.Round(taxableValue * (rate / 100m), 2);
            var cgst = Math.Round(taxAmount / 2m, 2);
            var sgst = taxAmount - cgst;
            return (cgst, sgst, taxAmount);
        }

        public static GstR1RowDto BuildRow(Order order, decimal rate, List<OrderItem> items, string? hotelGstin = null)
        {
            // Computed fresh as Price * Quantity rather than trusted from the stored Total field:
            // orders placed before the 2026-07-23 security fix (repricing lines from the item
            // catalog) had Total written as a tax-INCLUSIVE figure by the client, so summing Total
            // directly would double-count tax on every pre-fix invoice.
            var taxableValue = items.Sum(x => x.Price * x.Quantity);
            var taxAmount = Math.Round(taxableValue * (rate / 100m), 2);

            // Interstate (customer's GSTIN state differs from the hotel's own) is taxed as IGST;
            // everything else - including B2C rows with no customer GSTIN on file - defaults to the
            // safe intrastate CGST+SGST split, matching OrderService.CalculateTotals' own logic so
            // this report always reconciles with what was actually charged.
            bool isInterstate = GstinHelper.IsInterstate(hotelGstin, order.CustomerGstin);
            decimal cgst, sgst, igst;
            if (isInterstate)
            {
                cgst = 0m;
                sgst = 0m;
                igst = taxAmount;
            }
            else
            {
                (cgst, sgst, _) = ComputeTaxSplit(taxableValue, rate);
                igst = 0m;
            }

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
                Igst = igst
            };
        }

        /// <summary>The first two digits of a GSTIN are the issuing state's GST state code, which is
        /// what GSTR-1 filing tools show as "Place of Supply" for a B2B row.</summary>
        public static string DerivePlaceOfSupply(string? gstin) => GstinHelper.DeriveStateCode(gstin);

        /// <summary>Aggregates ALL outward supplies in the period (B2B and B2C combined, unlike the
        /// other tabs) into one row per HSN code and tax rate, matching GSTR-1 table 12 (HSN-wise
        /// summary). Items whose catalog entry has no HSN code set are grouped under "(No HSN)" so
        /// the gap stays visible in the report rather than being silently dropped.</summary>
        /// <param name="hotelGstin">The business's own GSTIN (Settings), used the same way as in
        /// <see cref="BuildRow"/> to split each item's tax into CGST+SGST or IGST before the
        /// per-HSN/rate totals below are summed - orders can't be split as a whole here since a
        /// single HSN/rate bucket mixes items from many orders with different customers.</param>
        public static List<HsnSummaryRowDto> BuildHsnSummary(IEnumerable<Order> orders, IReadOnlyDictionary<int, Item> catalogById, string? hotelGstin = null)
        {
            return orders
                .SelectMany(o => o.Items.Select(oi => (Order: o, Item: oi)))
                .GroupBy(x =>
                {
                    catalogById.TryGetValue(x.Item.ItemId, out var catalogItem);
                    var hsn = string.IsNullOrWhiteSpace(catalogItem?.HsnCode) ? "(No HSN)" : catalogItem!.HsnCode!;
                    return (Hsn: hsn, x.Item.TaxPercentage);
                })
                .Select(g =>
                {
                    var taxableValue = g.Sum(x => x.Item.Price * x.Item.Quantity);
                    var interstateTaxableValue = g
                        .Where(x => GstinHelper.IsInterstate(hotelGstin, x.Order.CustomerGstin))
                        .Sum(x => x.Item.Price * x.Item.Quantity);
                    var intrastateTaxableValue = taxableValue - interstateTaxableValue;

                    var (cgst, sgst, _) = ComputeTaxSplit(intrastateTaxableValue, g.Key.TaxPercentage);
                    var igst = Math.Round(interstateTaxableValue * (g.Key.TaxPercentage / 100m), 2);

                    var representative = g.First().Item;
                    catalogById.TryGetValue(representative.ItemId, out var catalogItem);
                    return new HsnSummaryRowDto
                    {
                        HsnCode = g.Key.Hsn,
                        Description = catalogItem?.Name ?? representative.ItemName,
                        Uqc = catalogItem?.Unit?.Name ?? string.Empty,
                        TotalQuantity = g.Sum(x => x.Item.Quantity),
                        TaxableValue = taxableValue,
                        Rate = g.Key.TaxPercentage,
                        Cgst = cgst,
                        Sgst = sgst,
                        Igst = igst
                    };
                })
                .OrderBy(r => r.HsnCode)
                .ThenBy(r => r.Rate)
                .ToList();
        }
    }
}
