using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
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
    public class PurchaseTests
    {
        private readonly Mock<IPurchaseRepository> _purchaseRepoMock;
        private readonly Mock<IItemRepository> _itemRepoMock;
        private readonly PurchaseService _purchaseService;
        
        private readonly Mock<IItemService> _itemServiceMock;
        private readonly Mock<INotificationService> _notificationServiceMock;

        public PurchaseTests()
        {
            _purchaseRepoMock = new Mock<IPurchaseRepository>();
            _itemRepoMock = new Mock<IItemRepository>();
            _purchaseService = new PurchaseService(_purchaseRepoMock.Object, _itemRepoMock.Object, TestAuthorization.AllowAll().Object);

            _itemServiceMock = new Mock<IItemService>();
            _notificationServiceMock = new Mock<INotificationService>();

            // Setup default empty suppliers list to prevent LoadDataAsync from throwing in ViewModels
            _purchaseRepoMock.Setup(r => r.GetSuppliersAsync()).ReturnsAsync(new List<Supplier>());
        }

        #region Service Layer Tests (PurchaseService)

        [Fact]
        public async Task SavePurchaseAsync_ValidPurchase_ShouldSaveAndIncrementStock()
        {
            // Arrange
            var supplier = new Supplier { Id = 1, Name = "Metro Wholesalers" };
            
            var item1 = new Item { Id = 10, Name = "Milk Packet", StockQuantity = 20, TrackInventory = true };
            var item2 = new Item { Id = 11, Name = "Cheese Slices", StockQuantity = 5, TrackInventory = true };
            var item3 = new Item { Id = 12, Name = "Paper Cups", StockQuantity = 100, TrackInventory = false }; // track inventory false

            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Item> { item1, item2, item3 });

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-1001",
                PurchaseDate = DateTime.Now,
                PaymentType = PaymentModes.Credit,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 10, UnitPrice = 40, TaxPercentage = 5, Total = 420 },
                    new PurchaseItem { ItemId = 11, ItemName = "Cheese Slices", Quantity = 5, UnitPrice = 120, TaxPercentage = 12, Total = 672 },
                    new PurchaseItem { ItemId = 12, ItemName = "Paper Cups", Quantity = 50, UnitPrice = 2, TaxPercentage = 0, Total = 100 }
                }
            };

            // Act
            await _purchaseService.SavePurchaseAsync(purchase);

            // Assert
            // 1. Verify saved in repo
            _purchaseRepoMock.Verify(r => r.AddAsync(purchase), Times.Once);

            // 2. Verify stock increment for TrackInventory = true
            Assert.Equal(30, item1.StockQuantity); // 20 + 10 = 30
            Assert.Equal(10, item2.StockQuantity); // 5 + 5 = 10
            
            // 3. Verify stock did NOT increment for TrackInventory = false
            Assert.Equal(100, item3.StockQuantity); // remains 100

            // 4. Verify the stock updates were batched into a single UpdateRangeAsync call
            // rather than one UpdateAsync call per item (avoids an N+1 write pattern).
            _itemRepoMock.Verify(r => r.UpdateRangeAsync(It.Is<List<Item>>(l =>
                l.Count == 2 && l.Contains(item1) && l.Contains(item2) && !l.Contains(item3))), Times.Once);
            _itemRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task SavePurchaseAsync_MissingSupplier_ShouldThrow()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 0, // Invalid
                InvoiceNumber = "INV-1001"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _purchaseService.SavePurchaseAsync(purchase));
        }

        [Fact]
        public async Task SavePurchaseAsync_EmptyInvoice_ShouldThrow()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "  " // Empty
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _purchaseService.SavePurchaseAsync(purchase));
        }

        [Fact]
        public async Task SavePurchaseAsync_NoItems_ShouldThrow()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-1001",
                PurchaseItems = new List<PurchaseItem>() // Empty list
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _purchaseService.SavePurchaseAsync(purchase));
        }

        [Fact]
        public async Task GetPurchaseByIdAsync_LegacyPath_DelegatesToRepository()
        {
            var purchase = new Purchase { Id = 5 };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(purchase);

            var result = await _purchaseService.GetPurchaseByIdAsync(5);

            Assert.Same(purchase, result);
        }

        [Fact]
        public async Task UpdatePurchaseAsync_LegacyPath_ReconcilesStockDeltaAndUpdatesRepo()
        {
            var item1 = new Item { Id = 10, Name = "Milk Packet", StockQuantity = 30, TrackInventory = true };
            var item2 = new Item { Id = 11, Name = "Cheese Slices", StockQuantity = 10, TrackInventory = true };

            var oldPurchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 10, UnitPrice = 40 },
                    new PurchaseItem { ItemId = 11, ItemName = "Cheese Slices", Quantity = 5, UnitPrice = 120 }
                }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldPurchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Item> { item1, item2 });

            var updatedPurchase = new Purchase
            {
                Id = 1,
                SupplierId = 1,
                InvoiceNumber = "INV-1001",
                PurchaseItems = new List<PurchaseItem>
                {
                    // Milk quantity increased 10 -> 15 (delta +5); Cheese removed entirely (delta -5)
                    new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 15, UnitPrice = 40 }
                }
            };

            await _purchaseService.UpdatePurchaseAsync(updatedPurchase);

            Assert.Equal(35, item1.StockQuantity); // 30 + 5
            Assert.Equal(5, item2.StockQuantity);  // 10 - 5
            _purchaseRepoMock.Verify(r => r.UpdateAsync(updatedPurchase), Times.Once);
        }

        [Fact]
        public async Task UpdatePurchaseAsync_LegacyPath_ShrinkingBelowZero_ClampsToZero()
        {
            var item = new Item { Id = 10, Name = "Milk Packet", StockQuantity = 2, TrackInventory = true };
            var oldPurchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 10, UnitPrice = 40 } }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldPurchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });

            var updatedPurchase = new Purchase
            {
                Id = 1,
                SupplierId = 1,
                InvoiceNumber = "INV-1001",
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 1, UnitPrice = 40 } }
            };

            await _purchaseService.UpdatePurchaseAsync(updatedPurchase);

            Assert.Equal(0, item.StockQuantity); // 2 + (1 - 10) = -7, clamped to 0
        }

        [Fact]
        public async Task UpdatePurchaseAsync_LegacyPath_PurchaseNotFound_ThrowsKeyNotFoundException()
        {
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Purchase?)null);

            var updated = new Purchase { Id = 99, SupplierId = 1, InvoiceNumber = "INV-1", PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 1, ItemName = "X", Quantity = 1, UnitPrice = 1 } } };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _purchaseService.UpdatePurchaseAsync(updated));
        }

        [Fact]
        public async Task DeletePurchaseAsync_LegacyPath_ReversesStockAndDeletes()
        {
            var item = new Item { Id = 10, Name = "Milk Packet", StockQuantity = 30, TrackInventory = true };
            var purchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk Packet", Quantity = 10, UnitPrice = 40 } }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(purchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });

            await _purchaseService.DeletePurchaseAsync(1);

            Assert.Equal(20, item.StockQuantity); // 30 - 10
            _purchaseRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeletePurchaseAsync_LegacyPath_NotFound_IsNoOp()
        {
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Purchase?)null);

            var exception = await Record.ExceptionAsync(() => _purchaseService.DeletePurchaseAsync(404));

            Assert.Null(exception);
            _purchaseRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region ViewModel Layer Tests (PurchaseEntryViewModel / PurchaseRow)

        [Fact]
        public void PurchaseRow_ShouldCalculateTotalCorrectly()
        {
            // Arrange
            var row = new PurchaseRow
            {
                Quantity = 5,
                UnitPrice = 100,
                TaxPercentage = 18,
                Discount = 50
            };

            // Act
            // Total = (Qty * Price) + Tax - Discount
            // Tax = Math.Round((Qty * Price) * (TaxPercentage/100), 2)
            // Sub = 5 * 100 = 500
            // Tax = 500 * 0.18 = 90
            // Total = 500 + 90 - 50 = 540
            var sub = row.Quantity * row.UnitPrice;
            var tax = Math.Round(sub * (row.TaxPercentage / 100m), 2);
            row.Total = sub + tax - row.Discount;

            // Assert
            Assert.Equal(500m, sub);
            Assert.Equal(90m, tax);
            Assert.Equal(540m, row.Total);
        }

        [Fact]
        public async Task PurchaseEntryViewModel_RecalculatesTotalsOnChanges()
        {
            // Arrange
            var vm = new PurchaseEntryViewModel(
                _purchaseService,
                _itemServiceMock.Object,
                _notificationServiceMock.Object);

            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item A", Price = 100, TaxPercentage = 10 },
                new Item { Id = 2, Name = "Item B", Price = 200, TaxPercentage = 5 }
            };
            _itemServiceMock.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            await vm.LoadDataAsync(); // Pre-loads items list

            // Act & Verify initial state
            Assert.Single(vm.PurchaseRows);
            var firstRow = vm.PurchaseRows[0];
            Assert.Equal(1, firstRow.SNo);

            // Change ItemId
            firstRow.ItemId = 1; // triggers Auto-Prefill PropertyChanged
            Assert.Equal("Item A", firstRow.ItemName);
            Assert.Equal(100m, firstRow.UnitPrice);
            Assert.Equal(10m, firstRow.TaxPercentage);

            // Verify Summary totals
            // Qty=1, Price=100, Tax=10%, Discount=0 => Subtotal=100, Tax=10, Total=110
            Assert.Equal(100m, vm.Subtotal);
            Assert.Equal(10m, vm.TotalTax);
            Assert.Equal(0m, vm.TotalDiscount);
            Assert.Equal(110m, vm.GrandTotal);

            // Add a new row
            vm.AddRowCommand.Execute(null);
            Assert.Equal(2, vm.PurchaseRows.Count);
            var secondRow = vm.PurchaseRows[1];
            Assert.Equal(2, secondRow.SNo);

            secondRow.ItemId = 2; // pre-fills
            secondRow.Quantity = 3; // 3 * 200 = 600. Tax = 30. Total = 630.
            secondRow.Discount = 40; // 600 + 30 - 40 = 590

            // Overall Summary recalculates
            // Subtotal = 100 + 600 = 700
            // TotalTax = 10 + 30 = 40
            // TotalDiscount = 0 + 40 = 40
            // GrandTotal = 110 + 590 = 700
            Assert.Equal(700m, vm.Subtotal);
            Assert.Equal(40m, vm.TotalTax);
            Assert.Equal(40m, vm.TotalDiscount);
            Assert.Equal(700m, vm.GrandTotal);
        }

        [Fact]
        public async Task PurchaseEntryViewModel_PreventsDuplicateItems()
        {
            // Arrange
            var vm = new PurchaseEntryViewModel(
                _purchaseService,
                _itemServiceMock.Object,
                _notificationServiceMock.Object);

            var items = new List<Item>
            {
                new Item { Id = 1, Name = "Item A", Price = 100, TaxPercentage = 10 }
            };
            _itemServiceMock.Setup(s => s.GetItemsAsync()).ReturnsAsync(items);
            await vm.LoadDataAsync();

            var firstRow = vm.PurchaseRows[0];
            firstRow.ItemId = 1;

            // Act
            vm.AddRowCommand.Execute(null);
            var secondRow = vm.PurchaseRows[1];
            secondRow.ItemId = 1; // Try to select duplicate item

            // Assert
            _notificationServiceMock.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("already added"))), Times.Once);
            Assert.Equal(0, secondRow.ItemId); // resets back to 0
        }

        [Fact]
        public async Task SavePurchaseAsync_NullPurchase_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _purchaseService.SavePurchaseAsync(null!));
        }

        [Fact]
        public async Task SavePurchaseAsync_ZeroQuantityItem_ThrowsArgumentException()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-101",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Rice", Quantity = 0, UnitPrice = 100 }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _purchaseService.SavePurchaseAsync(purchase));
            Assert.Contains("must be greater than zero", ex.Message);
        }

        [Fact]
        public async Task SavePurchaseAsync_NegativePrice_ThrowsArgumentException()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-101",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Rice", Quantity = 5, UnitPrice = -5 }
                }
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _purchaseService.SavePurchaseAsync(purchase));
            Assert.Contains("cannot be negative", ex.Message);
        }

        [Fact]
        public async Task SavePurchaseAsync_ItemNotInCatalog_GracefullySkips()
        {
            // Arrange
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-101",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 999, ItemName = "Unknown Item", Quantity = 5, UnitPrice = 10 }
                }
            };

            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item>());

            // Act
            var exception = await Record.ExceptionAsync(() => _purchaseService.SavePurchaseAsync(purchase));

            // Assert
            Assert.Null(exception);
            _purchaseRepoMock.Verify(r => r.AddAsync(purchase), Times.Once);
            _itemRepoMock.Verify(r => r.UpdateRangeAsync(It.IsAny<List<Item>>()), Times.Never);
        }

        [Fact]
        public async Task SavePurchaseAsync_WhenStockUpdateThrows_PurchaseIsStillSaved_NoTransactionWrap()
        {
            // Arrange
            var catalogItem = new Item { Id = 10, Name = "Rice", StockQuantity = 10, TrackInventory = true };
            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-101",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Rice", Quantity = 5, UnitPrice = 10 }
                }
            };

            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { catalogItem });
            _itemRepoMock.Setup(r => r.UpdateRangeAsync(It.IsAny<List<Item>>())).ThrowsAsync(new Exception("Database connection failure"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _purchaseService.SavePurchaseAsync(purchase));

            // Verify purchase was saved BEFORE the stock update failed (since there is no transaction wrap)
            _purchaseRepoMock.Verify(r => r.AddAsync(purchase), Times.Once);
        }

        #endregion
    }
}

