using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class DeletePurchaseCommandHandlerTests
    {
        private readonly Mock<IPurchaseRepository> _purchaseRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly DeletePurchaseCommandHandler _handler;

        public DeletePurchaseCommandHandlerTests()
        {
            _handler = new DeletePurchaseCommandHandler(_purchaseRepoMock.Object, _itemRepoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task Handle_PurchaseNotFound_IsNoOp()
        {
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Purchase?)null);

            await _handler.Handle(new DeletePurchaseCommand(404), CancellationToken.None);

            _purchaseRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
            _purchaseRepoMock.Verify(r => r.BeginTransactionAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ReversesStockAtomicallyForTrackedItemsOnlyAndDeletes()
        {
            var item1 = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var item2 = new Item { Id = 11, Name = "Cups", StockQuantity = 100, TrackInventory = false };
            var purchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 },
                    new PurchaseItem { ItemId = 11, ItemName = "Cups", Quantity = 50, UnitPrice = 2 }
                }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(purchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item1, item2 });

            await _handler.Handle(new DeletePurchaseCommand(1), CancellationToken.None);

            _itemRepoMock.Verify(r => r.AdjustStockAsync(10, -5), Times.Once);
            _itemRepoMock.Verify(r => r.AdjustStockAsync(11, It.IsAny<int>()), Times.Never); // TrackInventory = false
            _purchaseRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
            _purchaseRepoMock.Verify(r => r.CommitTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_StockUpdateThrows_RollsBackAndDoesNotDelete()
        {
            var item = new Item { Id = 10, Name = "Milk", StockQuantity = 20, TrackInventory = true };
            var purchase = new Purchase
            {
                Id = 1,
                PurchaseItems = new List<PurchaseItem> { new PurchaseItem { ItemId = 10, ItemName = "Milk", Quantity = 5, UnitPrice = 40 } }
            };
            _purchaseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(purchase);
            _itemRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(new List<Item> { item });
            _itemRepoMock.Setup(r => r.AdjustStockAsync(It.IsAny<int>(), It.IsAny<int>())).ThrowsAsync(new Exception("DB failure"));

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(new DeletePurchaseCommand(1), CancellationToken.None));

            _purchaseRepoMock.Verify(r => r.RollbackTransactionAsync(), Times.Once);
            _purchaseRepoMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
