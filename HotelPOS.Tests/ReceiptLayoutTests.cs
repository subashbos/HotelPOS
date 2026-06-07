using HotelPOS.Domain.Entities;
using System.Windows.Documents;
using Xunit;

namespace HotelPOS.Tests
{
    public class ReceiptLayoutTests
    {
        [Fact]
        public void CreateReceipt_Thermal_HasRateColumn()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var order = new Order
                {
                    Id = 1,
                    PaymentMode = "Cash",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ItemName = "Test Item", Price = 100, Quantity = 1, Total = 100 }
                    }
                };
                var settings = new SystemSetting { HotelName = "Test Hotel" };

                var doc = ReceiptGenerator.CreateReceipt(order, true, settings);

                var table = doc.Blocks.OfType<System.Windows.Documents.Table>().FirstOrDefault();
                Assert.NotNull(table);

                // Thermal columns: S.No (0), Item (1), Rate (2), Qty (3), Total (4)
                Assert.Equal(5, table.Columns.Count);

                var headerRow = table.RowGroups[0].Rows[0];
                var rateCell = headerRow.Cells[2];
                var rateText = ((Paragraph)rateCell.Blocks.First()).Inlines.OfType<Run>().First().Text;

                Assert.Equal("RATE", rateText);
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void CreateReceipt_Thermal_DoesNotContainTableInfo()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var order = new Order
                {
                    Id = 1,
                    TableNumber = 5,
                    PaymentMode = "UPI Payment",
                    Items = new List<OrderItem>()
                };
                var settings = new SystemSetting { HotelName = "Test Hotel" };

                var doc = ReceiptGenerator.CreateReceipt(order, true, settings);

                // Get all text from the document
                var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
                var text = textRange.Text;

                Assert.DoesNotContain("Table", text);
                Assert.DoesNotContain("UPI Payment", text);
                Assert.Contains("UPI", text); // Shortened payment mode
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void CreateReceipt_Thermal_QtyColumnWidthIsOptimized()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var order = new Order { Id = 1, Items = new List<OrderItem>() };
                var settings = new SystemSetting { HotelName = "Test Hotel" };

                var doc = ReceiptGenerator.CreateReceipt(order, true, settings);
                var table = doc.Blocks.OfType<System.Windows.Documents.Table>().FirstOrDefault();

                // Qty is index 3
                var qtyColumn = table!.Columns[3];
                Assert.Equal(0.9, qtyColumn.Width.Value);
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        [Fact]
        public void CreateKOT_Table0_ShowsTakeawayOnline()
        {
            var thread = new System.Threading.Thread(() =>
            {
                var items = new List<OrderItem> { new OrderItem { ItemName = "Burger", Quantity = 1 } };
                
                // Table 0 (Takeaway)
                var doc = ReceiptGenerator.CreateKOT(0, items, true);

                var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
                var text = textRange.Text;

                Assert.Contains("Table : Takeaway / Online", text);
                Assert.DoesNotContain("Table : 0", text);

                // Real Table 5
                var doc2 = ReceiptGenerator.CreateKOT(5, items, true);
                var text2 = new TextRange(doc2.ContentStart, doc2.ContentEnd).Text;
                Assert.Contains("Table : 5", text2);
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
