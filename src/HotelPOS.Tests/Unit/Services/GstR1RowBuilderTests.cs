using HotelPOS.Domain.Entities;
using HotelPOS.Views;
using Xunit;

namespace HotelPOS.Tests
{
    public class GstR1RowBuilderTests
    {
        private static Order MakeOrder(string? gstin = "33AQZPS2365E1ZE", string? invoiceNumber = "INV24", string? customerName = "Anu Labs", decimal totalAmount = 13227m) =>
            new()
            {
                CustomerGstin = gstin,
                InvoiceNumber = invoiceNumber,
                CustomerName = customerName,
                TotalAmount = totalAmount,
                CreatedAt = new DateTime(2020, 8, 8, 0, 0, 0, DateTimeKind.Utc)
            };

        [Fact]
        public void BuildRow_ComputesTaxableValueAndSplitsTaxEvenlyBetweenCgstAndSgst()
        {
            // Matches the reference GSTR-1 report's first row exactly: 6300 taxable @ 5%.
            var order = MakeOrder();
            var items = new List<OrderItem>
            {
                new() { Price = 6300, Quantity = 1, TaxPercentage = 5, Total = 6300 }
            };

            var row = GstR1RowBuilder.BuildRow(order, 5m, items);

            Assert.Equal(6300m, row.TaxableValue);
            Assert.Equal(315m, row.Cgst + row.Sgst);
            Assert.Equal(157.50m, row.Cgst);
            Assert.Equal(157.50m, row.Sgst);
            Assert.Equal(6615m, row.ItemTotal); // taxable + tax
        }

        [Fact]
        public void BuildRow_NeverPopulatesIgst_SinceTheAppOnlyEverChargesIntrastateTax()
        {
            var order = MakeOrder();
            var items = new List<OrderItem>
            {
                new() { Price = 2400, Quantity = 1, TaxPercentage = 18, Total = 2400 }
            };

            var row = GstR1RowBuilder.BuildRow(order, 18m, items);

            Assert.Equal(0m, row.Igst);
            Assert.True(row.Cgst > 0 && row.Sgst > 0);
        }

        [Fact]
        public void BuildRow_SumsMultipleLineItemsSharingTheSameRate()
        {
            var order = MakeOrder();
            var items = new List<OrderItem>
            {
                new() { Price = 100, Quantity = 2, TaxPercentage = 12, Total = 200 },
                new() { Price = 50, Quantity = 4, TaxPercentage = 12, Total = 200 }
            };

            var row = GstR1RowBuilder.BuildRow(order, 12m, items);

            Assert.Equal(400m, row.TaxableValue);
            Assert.Equal(48m, row.Cgst + row.Sgst); // 400 * 12% = 48
        }

        [Fact]
        public void BuildRow_CarriesInvoiceLevelFieldsFromTheOrder()
        {
            var order = MakeOrder(gstin: "33AQZPS2365E1ZE", invoiceNumber: "INV24", customerName: "Anu Labs", totalAmount: 13227m);
            var items = new List<OrderItem> { new() { Price = 100, Quantity = 1, TaxPercentage = 5, Total = 100 } };

            var row = GstR1RowBuilder.BuildRow(order, 5m, items);

            Assert.Equal("33AQZPS2365E1ZE", row.Gstin);
            Assert.Equal("INV24", row.InvoiceNumber);
            Assert.Equal("Anu Labs", row.CustomerName);
            Assert.Equal(13227m, row.InvoiceValue);
            Assert.Equal("N", row.ReverseCharge);
            Assert.Equal("R", row.InvoiceType);
            Assert.Equal(5m, row.Rate);
        }

        [Fact]
        public void BuildRow_NullCustomerGstinOrName_DoesNotThrowAndYieldsEmptyStrings()
        {
            var order = MakeOrder(gstin: null, customerName: null);
            var items = new List<OrderItem> { new() { Price = 100, Quantity = 1, TaxPercentage = 5, Total = 100 } };

            var row = GstR1RowBuilder.BuildRow(order, 5m, items);

            Assert.Equal(string.Empty, row.Gstin);
            Assert.Equal(string.Empty, row.CustomerName);
            Assert.Equal(string.Empty, row.Pos);
        }

        [Theory]
        [InlineData("33AQZPS2365E1ZE", "33")]
        [InlineData("29APWAS2365E1ZE", "29")]
        public void DerivePlaceOfSupply_ReturnsFirstTwoDigitsOfGstin(string gstin, string expectedPos)
        {
            Assert.Equal(expectedPos, GstR1RowBuilder.DerivePlaceOfSupply(gstin));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("3")]
        public void DerivePlaceOfSupply_MissingOrTooShortGstin_ReturnsEmptyString(string? gstin)
        {
            Assert.Equal(string.Empty, GstR1RowBuilder.DerivePlaceOfSupply(gstin));
        }

        [Fact]
        public void BuildRows_ExpandsEachOrderIntoOneRowPerDistinctTaxRate()
        {
            var order = MakeOrder();
            order.Items =
            [
                new() { Price = 100, Quantity = 1, TaxPercentage = 5, Total = 100 },
                new() { Price = 200, Quantity = 1, TaxPercentage = 18, Total = 200 }
            ];

            var rows = GstR1RowBuilder.BuildRows([order]);

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, r => r.Rate == 5m && r.TaxableValue == 100m);
            Assert.Contains(rows, r => r.Rate == 18m && r.TaxableValue == 200m);
        }

        [Fact]
        public void BuildB2cSummary_AggregatesTaxableValueAndTaxAcrossOrdersSharingARate()
        {
            var order1 = MakeOrder(invoiceNumber: "INV1");
            order1.Items = [new() { Price = 6300, Quantity = 1, TaxPercentage = 5, Total = 6300 }];

            var order2 = MakeOrder(invoiceNumber: "INV2");
            order2.Items = [new() { Price = 1000, Quantity = 1, TaxPercentage = 5, Total = 1000 }];

            var summary = GstR1RowBuilder.BuildB2cSummary([order1, order2]);

            var bucket = Assert.Single(summary);
            Assert.Equal(5m, bucket.Rate);
            Assert.Equal(2, bucket.InvoiceCount);
            Assert.Equal(7300m, bucket.TaxableValue);
            Assert.Equal(365m, bucket.TotalTax); // 7300 * 5%
            Assert.Equal(7665m, bucket.TotalValue);
            Assert.Equal(0m, bucket.Igst);
        }

        [Fact]
        public void BuildB2cSummary_SeparatesRatesIntoDistinctRowsOrderedByRate()
        {
            var order = MakeOrder();
            order.Items =
            [
                new() { Price = 100, Quantity = 1, TaxPercentage = 18, Total = 100 },
                new() { Price = 200, Quantity = 1, TaxPercentage = 5, Total = 200 }
            ];

            var summary = GstR1RowBuilder.BuildB2cSummary([order]);

            Assert.Equal(2, summary.Count);
            Assert.Equal(5m, summary[0].Rate);
            Assert.Equal(18m, summary[1].Rate);
        }
    }
}
