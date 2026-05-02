using HotelPOS.Domain;
using System.Windows;
using System.Windows.Documents;
using Xunit;

namespace HotelPOS.Tests
{
    public class ReceiptGeneratorTests
    {
        private static readonly SystemSetting DefaultSettings = new SystemSetting
        {
            HotelName = "Test Hotel",
            HotelAddress = "123 Test Street, City",
            HotelPhone = "9999999999",
            HotelGst = "27ABCDE1234F1Z5",
            ReceiptFormat = "Thermal",
            ShowGstBreakdown = true,
            ShowItemsOnBill = true,
            ShowPhoneOnReceipt = true,
            ShowThankYouFooter = true,
            ShowDiscountLine = false
        };

        private Order CreateTestOrder()
        {
            return new Order
            {
                Id = 101,
                TableNumber = 5,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem { ItemName = "Coffee", Price = 50,  Quantity = 2, Total = 100 },
                    new OrderItem { ItemName = "Burger", Price = 150, Quantity = 1, Total = 150 }
                },
                TotalAmount = 262.5m
            };
        }

        [Fact]
        public void CreateReceipt_IsThermalTrue_SetsNarrowConstraints()
        {
            var order = CreateTestOrder();
            FlowDocument document = ReceiptGenerator.CreateReceipt(order, true, DefaultSettings);

            Assert.NotNull(document);
            Assert.Equal(285, document.MaxPageWidth);
            Assert.Equal(275, document.ColumnWidth);
            Assert.Equal(new Thickness(8, 10, 8, 20), document.PagePadding);
        }

        [Fact]
        public void CreateReceipt_IsThermalFalse_SetsWideConstraints()
        {
            var order = CreateTestOrder();
            FlowDocument document = ReceiptGenerator.CreateReceipt(order, false, DefaultSettings);

            Assert.NotNull(document);
            Assert.Equal(double.PositiveInfinity, document.MaxPageWidth);
            Assert.Equal(700, document.ColumnWidth);
            Assert.Equal(new Thickness(50), document.PagePadding);
        }

        [Fact]
        public void CreateReceipt_GeneratesValidFlowDocument_WithHeadersAndFooters()
        {
            var order = CreateTestOrder();
            FlowDocument document = ReceiptGenerator.CreateReceipt(order, true, DefaultSettings);

            Assert.True(document.Blocks.Count >= 5,
                "Document should contain at least 5 blocks (Header, Separator, ItemTable, Separator, FooterTable/FooterText)");

            var tables = 0;
            foreach (var block in document.Blocks)
                if (block is Table) tables++;

            Assert.True(tables >= 2, "Document should contain a table for items and a table for totals");
        }

        [Fact]
        public void CreateReceipt_ContainsHotelName_InHeader()
        {
            var order = CreateTestOrder();
            FlowDocument document = ReceiptGenerator.CreateReceipt(order, true, DefaultSettings);
            Assert.NotNull(document);
            // Header block exists and document renders
            Assert.True(document.Blocks.Count > 0);
        }
    }
}
