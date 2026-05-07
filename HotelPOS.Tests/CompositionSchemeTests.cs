using HotelPOS.Application.Interface;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain;
using HotelPOS.ViewModels;
using Moq;
using System.Windows.Documents;
using Xunit;

namespace HotelPOS.Tests
{
    public class CompositionSchemeTests
    {
        private readonly Mock<IItemService> _itemService = new();
        private readonly Mock<ICartService> _cartService = new();
        private readonly Mock<IOrderService> _orderService = new();
        private readonly Mock<ISettingService> _settingService = new();
        private readonly Mock<ICategoryService> _categoryService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<ICashService> _cashService = new();

        [Fact]
        public async Task UpdateCart_ZerosGst_WhenCompositionSchemeActive()
        {
            // Arrange
            _cartService.Setup(s => s.GetHeldOrders()).Returns(new List<HeldOrder>());
            _settingService.Setup(s => s.GetSettingsAsync()).ReturnsAsync(new SystemSetting { IsCompositionScheme = true });
            _itemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());
            _categoryService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<Category>());
            _cashService.Setup(s => s.GetCurrentSessionAsync()).ReturnsAsync(new CashSession());
            _cartService.Setup(s => s.GetItems(It.IsAny<int>())).Returns(new List<OrderItem>());
            _cartService.Setup(s => s.GetSubtotal(It.IsAny<int>())).Returns(100m);
            _cartService.Setup(s => s.GetGstAmount(It.IsAny<int>())).Returns(5m); // 5% GST

            var vm = new BillingViewModel(
                _itemService.Object,
                _cartService.Object,
                _orderService.Object,
                _settingService.Object,
                _categoryService.Object,
                _notificationService.Object,
                _cashService.Object);

            // Act
            await vm.InitializeAsync(); // This loads settings and calls UpdateCart

            // Assert
            Assert.True(vm.IsCompositionScheme);
            Assert.Equal(100m, vm.Subtotal);
            Assert.Equal(0m, vm.GstAmount); // GST should be zeroed in VM
            Assert.Equal(100m, vm.TotalAmount);
        }

        [Fact]
        public void CreateReceipt_UsesBillOfSupplyTitle_InCompositionMode()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var order = new Order
                {
                    Id = 123,
                    Items = new List<OrderItem> { new OrderItem { ItemName = "Burger", Price = 100, Quantity = 1, Total = 100, TaxPercentage = 5 } },
                    TotalAmount = 100
                };
                var settings = new SystemSetting
                {
                    IsCompositionScheme = true,
                    ShowGstBreakdown = true // Even if breakdown is enabled in settings, it should be hidden in composition mode
                };

                // Act
                var doc = ReceiptGenerator.CreateReceipt(order, true, settings);

                // Assert
                var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
                var text = textRange.Text;

                Assert.Contains("BILL OF SUPPLY", text);
                Assert.DoesNotContain("TAX INVOICE", text);
                Assert.DoesNotContain("CGST", text);
                Assert.DoesNotContain("SGST", text);
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        [Fact]
        public void CreateReceipt_UsesTaxInvoiceTitle_InRegularMode()
        {
            var thread = new System.Threading.Thread(() =>
            {
                // Arrange
                var order = new Order
                {
                    Id = 123,
                    Items = new List<OrderItem> { new OrderItem { ItemName = "Burger", Price = 100, Quantity = 1, Total = 100, TaxPercentage = 5 } },
                    GstAmount = 5,
                    TotalAmount = 105
                };
                var settings = new SystemSetting
                {
                    IsCompositionScheme = false,
                    ShowGstBreakdown = true
                };

                // Act
                var doc = ReceiptGenerator.CreateReceipt(order, true, settings);

                // Assert
                var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
                var text = textRange.Text;

                Assert.Contains("TAX INVOICE", text);
                Assert.DoesNotContain("BILL OF SUPPLY", text);
                Assert.Contains("CGST", text);
                Assert.Contains("SGST", text);
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
