using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class PurchaseEntryViewModelTests
    {
        private readonly Mock<IPurchaseService> _mockPurchaseService = new();
        private readonly Mock<IItemService> _mockItemService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly PurchaseEntryViewModel _vm;

        public PurchaseEntryViewModelTests()
        {
            _mockPurchaseService.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(new List<Supplier>());
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(new List<Item>());

            _vm = new PurchaseEntryViewModel(
                _mockPurchaseService.Object,
                _mockItemService.Object,
                _mockNotif.Object
            );
        }

        [Fact]
        public async Task LoadDataAsync_LoadsSuppliersAndItems()
        {
            // Arrange
            var suppliers = new List<Supplier> { new() { Id = 1, Name = "Sup1" } };
            var items = new List<Item> { new() { Id = 1, Name = "Item1", Price = 100m, TaxPercentage = 5m } };

            _mockPurchaseService.Setup(s => s.GetSuppliersAsync()).ReturnsAsync(suppliers);
            _mockItemService.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);

            // Act
            await _vm.LoadDataAsync();

            // Assert
            Assert.Single(_vm.Suppliers);
            Assert.Equal("Sup1", _vm.Suppliers[0].Name);
            Assert.Single(_vm.CatalogItems);
            Assert.Equal("Item1", _vm.CatalogItems[0].Name);
            Assert.Single(_vm.PurchaseRows); // Initializes with one default row
        }

        [Fact]
        public async Task AddRow_IncreasesRowCountAndSetsSNo()
        {
            await _vm.InitializationTask;

            // Act
            _vm.AddRowCommand.Execute(null);

            // Assert
            Assert.Equal(2, _vm.PurchaseRows.Count);
            Assert.Equal(1, _vm.PurchaseRows[0].SNo);
            Assert.Equal(2, _vm.PurchaseRows[1].SNo);
        }

        [Fact]
        public async Task RemoveRow_RemovesSelectedRow()
        {
            await _vm.InitializationTask;

            // Arrange
            _vm.AddRowCommand.Execute(null);
            var secondRow = _vm.PurchaseRows[1];

            // Act
            _vm.RemoveRowCommand.Execute(secondRow);

            // Assert
            Assert.Single(_vm.PurchaseRows);
        }

        [Fact]
        public async Task RowPropertiesChanged_RecalculatesTotals()
        {
            await _vm.InitializationTask;

            // Arrange
            _vm.CatalogItems.Add(new Item { Id = 1, Name = "Item1", Price = 100m, TaxPercentage = 10m });
            var row = _vm.PurchaseRows[0];

            // Act - Item selected
            row.ItemId = 1;
            row.Quantity = 2; // 2 * 100 = 200 subtotal, 20 tax, 0 discount

            // Assert
            Assert.Equal("Item1", row.ItemName);
            Assert.Equal(100m, row.UnitPrice);
            Assert.Equal(10m, row.TaxPercentage);
            Assert.Equal(220m, row.Total);

            Assert.Equal(200m, _vm.Subtotal);
            Assert.Equal(20m, _vm.TotalTax);
            Assert.Equal(220m, _vm.GrandTotal);
        }

        [Fact]
        public async Task SavePurchaseCommand_ValidationFails_ShowsWarning()
        {
            await _vm.InitializationTask;

            // Arrange
            _vm.SelectedSupplier = null; // Fails validation

            // Act
            await _vm.SavePurchaseCommand.ExecuteAsync(null);

            // Assert
            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockPurchaseService.Verify(s => s.SavePurchaseAsync(It.IsAny<Purchase>()), Times.Never);
        }

        [Fact]
        public async Task Cancel_ResetsFormToDefault()
        {
            await _vm.InitializationTask;

            // Arrange
            _vm.SelectedSupplier = new Supplier { Id = 1, Name = "Sup" };
            _vm.InvoiceNumber = "INV123";

            // Act
            _vm.CancelCommand.Execute(null);

            // Assert
            Assert.Null(_vm.SelectedSupplier);
            Assert.Empty(_vm.InvoiceNumber);
            Assert.Single(_vm.PurchaseRows);
        }
    }
}

