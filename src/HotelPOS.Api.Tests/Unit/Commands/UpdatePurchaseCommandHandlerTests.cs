using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class UpdatePurchaseCommandHandlerTests
    {
        private readonly Mock<IPurchaseRepository> _purchaseRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly UpdatePurchaseCommandHandler _handler;

        public UpdatePurchaseCommandHandlerTests()
        {
            _handler = new UpdatePurchaseCommandHandler(_purchaseRepoMock.Object, _itemRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_PurchaseNotFound_ThrowsKeyNotFoundException()
        {
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Purchase?)null);
            var purchase = new Purchase { Id = 99, PurchaseItems = new List<PurchaseItem>() };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _handler.Handle(new UpdatePurchaseCommand(purchase), CancellationToken.None));
        }

        [Fact]
        public async Task Handle_QuantityIncreased_AppliesPositiveDeltaAtomicallyAndUpdates()
        {
            var item = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var oldPurchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 } }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldPurchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });

            var updated = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 8, UnitPrice = 40 } }
            };

            await _handler.Handle(new UpdatePurchaseCommand(updated), CancellationToken.None);

            _itemRepoMock.Verify(r => r.AdjustStockAsync(10, 3), Times.Once); // delta = 8 - 5
            _purchaseRepoMock.Verify(r => r.UpdateAsync(updated), Times.Once);
            _purchaseRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ItemRemovedEntirely_ReversesItsStockContributionAtomically()
        {
            var item1 = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var item2 = new Item { Id = 11, Name = "Cheese", StockQuantity = 8, TrackInventory = true };
            var oldPurchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 },
                    new PurchaseItem { ItemId = 11, ItemName = "Cheese", Quantity = 3, UnitPrice = 120 }
                }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldPurchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item1, item2 });

            var updated = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 } }
            };

            await _handler.Handle(new UpdatePurchaseCommand(updated), CancellationToken.None);

            _itemRepoMock.Verify(r => r.AdjustStockAsync(10, It.IsAny<int>()), Times.Never); // delta 0, skipped entirely
            _itemRepoMock.Verify(r => r.AdjustStockAsync(11, -3), Times.Once); // fully removed: 0 - 3
        }

        [Fact]
        public async Task Handle_StockUpdateThrows_RollsBackTransaction()
        {
            var item = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var oldPurchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 } }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(oldPurchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });
            _itemRepoMock.Setup(r => r.AdjustStockAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB failure"));

            var updated = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 8, UnitPrice = 40 } }
            };

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(new UpdatePurchaseCommand(updated), CancellationToken.None));

            _purchaseRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _purchaseRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Never);
            _purchaseRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Purchase>()), Times.Never);
        }
    }
}
