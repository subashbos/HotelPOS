using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class SavePurchaseCommandHandlerTests
    {
        private readonly Mock<IPurchaseRepository> _purchaseRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly SavePurchaseCommandHandler _handler;

        public SavePurchaseCommandHandlerTests()
        {
            _handler = new SavePurchaseCommandHandler(_purchaseRepoMock.Object, _itemRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_ValidPurchase_AtomicallyAdjustsStockForTrackedItemsOnly()
        {
            var item1 = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var item2 = new Item { Id = 11, Name = "Cups", StockQuantity = 100, TrackInventory = false };

            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<Item> { item1, item2 });

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-1",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 },
                    new PurchaseItem { ItemId = 11, ItemName = "Cups", Quantity = 50, UnitPrice = 2 }
                }
            };

            await _handler.Handle(new SavePurchaseCommand(purchase), CancellationToken.None);

            _purchaseRepoMock.Verify(r => r.AddAsync(purchase), Times.Once);
            _itemRepoMock.Verify(r => r.AdjustStockAsync(10, 5), Times.Once);
            _itemRepoMock.Verify(r => r.AdjustStockAsync(11, It.IsAny<int>()), Times.Never); // TrackInventory = false
            _purchaseRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ItemNotInCatalog_GracefullySkipsWithoutAdjustingStock()
        {
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item>());

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-2",
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 999, ItemName = "Unknown", Quantity = 5, UnitPrice = 10 }
                }
            };

            var exception = await Record.ExceptionAsync(() => _handler.Handle(new SavePurchaseCommand(purchase), CancellationToken.None));

            Assert.Null(exception);
            _itemRepoMock.Verify(r => r.AdjustStockAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StockUpdateThrows_RollsBackTransaction()
        {
            var item = new Item { Id = 10, Name = "Rice", StockQuantity = 10, TrackInventory = true };
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });
            _itemRepoMock.Setup(r => r.AdjustStockAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB failure"));

            var purchase = new Purchase
            {
                SupplierId = 1,
                InvoiceNumber = "INV-3",
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Rice", Quantity = 5, UnitPrice = 10 } }
            };

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(new SavePurchaseCommand(purchase), CancellationToken.None));

            _purchaseRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _purchaseRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Never);
        }
    }
}
