using HotelPOS.Application.UseCases.Reports;
using HotelPOS.Domain.Entities;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>Covers the Application-layer GstR1RowBuilder (used by the API/Angular GSTR-1
    /// report). Mirrors GstR1RowBuilderTests, which covers the separate WPF-only copy of this
    /// same logic in HotelPOS.Views.</summary>
    public class GstR1ReportBuilderTests
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
            Assert.Equal(6615m, row.ItemTotal);
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
            Assert.Equal(365m, bucket.TotalTax);
            Assert.Equal(7665m, bucket.TotalValue);
            Assert.Equal(0m, bucket.Igst);
        }

        [Fact]
        public void BuildHsnSummary_GroupsByHsnCodeAndRate_AcrossBothB2BAndB2COrders()
        {
            var catalog = new Dictionary<int, Item>
            {
                [1] = new Item { Id = 1, Name = "Chicken Biriyani", HsnCode = "2106", TaxPercentage = 5, Unit = new UnitOfMeasurement { Name = "Plate" } }
            };
            var b2bOrder = MakeOrder(gstin: "33AQZPS2365E1ZE", invoiceNumber: "INV1");
            b2bOrder.Items = [new() { ItemId = 1, ItemName = "Chicken Biriyani", Price = 100, Quantity = 2, TaxPercentage = 5, Total = 200 }];

            var b2cOrder = MakeOrder(gstin: null, invoiceNumber: "INV2");
            b2cOrder.Items = [new() { ItemId = 1, ItemName = "Chicken Biriyani", Price = 100, Quantity = 1, TaxPercentage = 5, Total = 100 }];

            var summary = GstR1RowBuilder.BuildHsnSummary([b2bOrder, b2cOrder], catalog);

            var row = Assert.Single(summary);
            Assert.Equal("2106", row.HsnCode);
            Assert.Equal("Chicken Biriyani", row.Description);
            Assert.Equal("Plate", row.Uqc);
            Assert.Equal(3, row.TotalQuantity);
            Assert.Equal(300m, row.TaxableValue);
            Assert.Equal(15m, row.TotalTax);
        }

        [Fact]
        public void BuildHsnSummary_ItemsWithoutHsnCode_GroupUnderNoHsnPlaceholder()
        {
            var catalog = new Dictionary<int, Item>
            {
                [1] = new Item { Id = 1, Name = "Mystery Item", HsnCode = null, TaxPercentage = 12 }
            };
            var order = MakeOrder();
            order.Items = [new() { ItemId = 1, ItemName = "Mystery Item", Price = 50, Quantity = 1, TaxPercentage = 12, Total = 50 }];

            var summary = GstR1RowBuilder.BuildHsnSummary([order], catalog);

            var row = Assert.Single(summary);
            Assert.Equal("(No HSN)", row.HsnCode);
        }
    }
}
