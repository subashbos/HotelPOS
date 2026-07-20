using HotelPOS.Domain.Common.Constants;
using HotelPOS.Application.DTOs.Report;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests
{
    public class PurchaseReportViewModelTests
    {
        private readonly Mock<IReportService> _reportServiceMock;
        private readonly Mock<IPurchaseService> _purchaseServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public PurchaseReportViewModelTests()
        {
            _reportServiceMock = new Mock<IReportService>();
            _purchaseServiceMock = new Mock<IPurchaseService>();
            _notificationServiceMock = new Mock<INotificationService>();
        }

        [Fact]
        public async Task InitializeAsync_LoadsSuppliersAndInitialReportData()
        {
            // Arrange
            var suppliers = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Supplier A" },
                new Supplier { Id = 2, Name = "Supplier B" }
            };

            var reportRows = new List<PurchaseReportRowDto>
            {
                new PurchaseReportRowDto
                {
                    PurchaseDate = DateTime.Now,
                    InvoiceNumber = "PUR001",
                    SupplierName = "Supplier A",
                    ItemName = "Tomato",
                    Quantity = 10,
                    UnitPrice = 25,
                    TaxAmount = 5,
                    Discount = 2,
                    TotalAmount = 253,
                    PaymentType = PaymentModes.Cash
                }
            };

            _purchaseServiceMock.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(suppliers);
            _reportServiceMock.Setup(r => r.GetPagedPurchaseReportAsync(It.IsAny<PagedPurchaseReportRequest>()))
                .ReturnsAsync((reportRows, 1, 253m, 5m, 2m, 10));

            var vm = new PurchaseReportViewModel(_reportServiceMock.Object, _purchaseServiceMock.Object, _notificationServiceMock.Object);

            // Act
            await vm.InitializeAsync();

            // Assert
            Assert.Equal(3, vm.Suppliers.Count); // "All Suppliers" + 2 suppliers
            Assert.Equal("All Suppliers", vm.Suppliers[0].Name);
            Assert.Equal("Supplier A", vm.Suppliers[1].Name);
            Assert.Single(vm.ReportRows);
            Assert.Equal("Tomato", vm.ReportRows[0].ItemName);
            Assert.Equal(253m, vm.TotalPurchaseAmount);
            Assert.Equal(5m, vm.TotalTax);
            Assert.Equal(2m, vm.TotalDiscount);
            Assert.Equal(10, vm.TotalQuantity);
            Assert.Equal(1, vm.TotalPurchasesCount);
        }

        [Fact]
        public async Task ApplyFilterAsync_TriggersLoadWithFilters()
        {
            // Arrange
            var suppliers = new List<Supplier> { new Supplier { Id = 1, Name = "Supplier A" } };
            _purchaseServiceMock.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(suppliers);

            var reportRows = new List<PurchaseReportRowDto>();
            _reportServiceMock.Setup(r => r.GetPagedPurchaseReportAsync(It.IsAny<PagedPurchaseReportRequest>()))
                .ReturnsAsync((reportRows, 0, 0m, 0m, 0m, 0));

            var vm = new PurchaseReportViewModel(_reportServiceMock.Object, _purchaseServiceMock.Object, _notificationServiceMock.Object);
            await vm.InitializeAsync();

            // Set filter inputs
            var targetFrom = DateTime.Today.AddDays(-5);
            var targetTo = DateTime.Today;
            vm.FilterFrom = targetFrom;
            vm.FilterTo = targetTo;
            vm.SelectedSupplier = vm.Suppliers[1]; // Supplier A
            vm.ItemNameSearch = "Onions";
            vm.InvoiceNoSearch = "INV123";
            vm.SelectedPaymentType = "Credit";

            // Act
            await vm.ApplyFilterAsync();

            // Assert
            _reportServiceMock.Verify(r => r.GetPagedPurchaseReportAsync(It.Is<PagedPurchaseReportRequest>(req => 
                req.Page == 1 && req.PageSize == 20 && req.From == targetFrom && req.To == targetTo.AddDays(1) &&
                req.SupplierId == 1 && req.ItemName == "Onions" && req.PaymentType == "Credit" && req.InvoiceNo == "INV123")), Times.Once);
        }

        [Fact]
        public async Task ResetFilterAsync_RestoresDefaultsAndReloads()
        {
            // Arrange
            var suppliers = new List<Supplier> { new Supplier { Id = 1, Name = "Supplier A" } };
            _purchaseServiceMock.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(suppliers);

            _reportServiceMock.Setup(r => r.GetPagedPurchaseReportAsync(It.IsAny<PagedPurchaseReportRequest>()))
                .ReturnsAsync((new List<PurchaseReportRowDto>(), 0, 0m, 0m, 0m, 0));

            var vm = new PurchaseReportViewModel(_reportServiceMock.Object, _purchaseServiceMock.Object, _notificationServiceMock.Object);
            await vm.InitializeAsync();

            // Change VM filters
            vm.FilterFrom = DateTime.Today.AddDays(-10);
            vm.FilterTo = DateTime.Today.AddDays(-5);
            vm.SelectedSupplier = vm.Suppliers[1];
            vm.ItemNameSearch = "Garlic";
            vm.InvoiceNoSearch = "INV555";
            vm.SelectedPaymentType = PaymentModes.Upi;

            // Act
            await vm.ResetFilterAsync();

            // Assert
            Assert.Equal(DateTime.Today, vm.FilterFrom);
            Assert.Equal(DateTime.Today, vm.FilterTo);
            Assert.Equal("All Suppliers", vm.SelectedSupplier?.Name);
            Assert.Equal(string.Empty, vm.ItemNameSearch);
            Assert.Equal(string.Empty, vm.InvoiceNoSearch);
            Assert.Equal("All", vm.SelectedPaymentType);
        }

        [Fact]
        public async Task ExportReadiness_VerifiesReportRowsContainExpectedExcelProperties()
        {
            // Arrange
            var row = new PurchaseReportRowDto
            {
                PurchaseDate = new DateTime(2026, 6, 9, 10, 0, 0),
                InvoiceNumber = "PUR-EX",
                SupplierName = "Super Mart",
                ItemName = "Apples",
                Quantity = 50,
                UnitPrice = 12.5m,
                TaxAmount = 6.25m,
                Discount = 1.0m,
                TotalAmount = 630.25m,
                PaymentType = "Credit"
            };

            _purchaseServiceMock.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(new List<Supplier>());
            _reportServiceMock.Setup(r => r.GetPagedPurchaseReportAsync(It.IsAny<PagedPurchaseReportRequest>()))
                .ReturnsAsync((new List<PurchaseReportRowDto> { row }, 1, 630.25m, 6.25m, 1.0m, 50));

            var vm = new PurchaseReportViewModel(_reportServiceMock.Object, _purchaseServiceMock.Object, _notificationServiceMock.Object);
            await vm.InitializeAsync();

            // Act
            var exportItem = vm.ReportRows.FirstOrDefault();

            // Assert
            Assert.NotNull(exportItem);
            Assert.Equal("PUR-EX", exportItem.InvoiceNumber);
            Assert.Equal("Super Mart", exportItem.SupplierName);
            Assert.Equal("Apples", exportItem.ItemName);
            Assert.Equal(50, exportItem.Quantity);
            Assert.Equal(12.5m, exportItem.UnitPrice);
            Assert.Equal(6.25m, exportItem.TaxAmount);
            Assert.Equal(1.0m, exportItem.Discount);
            Assert.Equal(630.25m, exportItem.TotalAmount);
            Assert.Equal("Credit", exportItem.PaymentType);
        }

        [Fact]
        public async Task InitializeAsync_WhenServiceThrows_ShowsNotificationError()
        {
            // Arrange
            _purchaseServiceMock.Setup(s => s.GetSuppliersAsync()).ThrowsAsync(new Exception("Database connection lost"));
            var vm = new PurchaseReportViewModel(_reportServiceMock.Object, _purchaseServiceMock.Object, _notificationServiceMock.Object);

            // Act
            await vm.InitializeAsync();

            // Assert
            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to initialize report") && s.Contains("Database connection lost"))), Times.Once);
        }
    }
}

