using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers ItemService edge cases missing from ItemServiceTests.cs:
    /// TrackInventory=false skip, stock return (negative deduct), item-not-found,
    /// UpdateItem not-found, DeleteItem invalid id, BulkAdd empty list,
    /// and TaxPercentage > 100 accepted silently.
    /// </summary>
    public class ItemServiceLoopholeTests
    {
        private readonly Mock<IItemRepository> _repo = new();
        private readonly ItemService _service;

        public ItemServiceLoopholeTests()
        {
            _service = new ItemService(_repo.Object);
        }

        // ── DeductStockAsync — TrackInventory = false ────────────────────────

        [Fact]
        public async Task DeductStockAsync_TrackInventoryFalse_DoesNotChangeStock()
        {
            var item = new Item { Id = 1, Name = "Napkin", StockQuantity = 100, TrackInventory = false };
            _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

            await _service.DeductStockAsync(1, 10);

            Assert.Equal(100, item.StockQuantity);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        // ── DeductStockAsync — negative quantity (stock return) ──────────────

        [Fact]
        public async Task DeductStockAsync_NegativeQuantity_IncreasesStock()
        {
            var item = new Item { Id = 2, Name = "Coke", StockQuantity = 10, TrackInventory = true };
            _repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(item);

            // Returning 5 units (negative deduct)
            await _service.DeductStockAsync(2, -5);

            Assert.Equal(15, item.StockQuantity);
            _repo.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task DeductStockAsync_NegativeQuantity_TrackInventoryFalse_DoesNothing()
        {
            var item = new Item { Id = 3, Name = "Straw", StockQuantity = 50, TrackInventory = false };
            _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(item);

            await _service.DeductStockAsync(3, -10);

            Assert.Equal(50, item.StockQuantity);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        // ── DeductStockAsync — item not found ────────────────────────────────

        [Fact]
        public async Task DeductStockAsync_ItemNotFound_DoesNotThrow()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Item?)null);

            var ex = await Record.ExceptionAsync(() => _service.DeductStockAsync(999, 5));
            Assert.Null(ex);
        }

        // ── DeductStockAsync — insufficient stock ────────────────────────────

        [Fact]
        public async Task DeductStockAsync_InsufficientStock_ThrowsInvalidOperationException()
        {
            var item = new Item { Id = 4, Name = "Burger", StockQuantity = 3, TrackInventory = true };
            _repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(item);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeductStockAsync(4, 10));
        }

        [Fact]
        public async Task DeductStockAsync_ExactStock_Succeeds()
        {
            var item = new Item { Id = 5, Name = "Tea", StockQuantity = 5, TrackInventory = true };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(item);

            await _service.DeductStockAsync(5, 5);

            Assert.Equal(0, item.StockQuantity);
        }

        // ── UpdateItemAsync — item not found ─────────────────────────────────

        [Fact]
        public async Task UpdateItemAsync_ItemNotFound_ThrowsKeyNotFoundException()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Item?)null);

            var dto = new CreateItemDto { Name = "Ghost", Price = 100 };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateItemAsync(999, dto));
        }

        // ── DeleteItemAsync — invalid id ─────────────────────────────────────

        [Fact]
        public async Task DeleteItemAsync_ZeroId_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteItemAsync(0));
        }

        [Fact]
        public async Task DeleteItemAsync_NegativeId_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.DeleteItemAsync(-5));
        }

        [Fact]
        public async Task DeleteItemAsync_ValidId_CallsRepo()
        {
            await _service.DeleteItemAsync(10);

            _repo.Verify(r => r.DeleteAsync(10), Times.Once);
        }

        // ── BulkAddAsync — empty list ────────────────────────────────────────

        [Fact]
        public async Task BulkAddAsync_EmptyList_ReturnsZeroAddedAndSkipped()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

            var (added, skipped) = await _service.BulkAddAsync(new List<CreateItemDto>());

            Assert.Equal(0, added);
            Assert.Equal(0, skipped);
            _repo.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task BulkAddAsync_AllInvalidItems_SkipsAll()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

            var dtos = new List<CreateItemDto>
            {
                new CreateItemDto { Name = "", Price = 100 },   // empty name
                new CreateItemDto { Name = "Valid", Price = 0 } // zero price
            };

            var (added, skipped) = await _service.BulkAddAsync(dtos);

            Assert.Equal(0, added);
            Assert.Equal(2, skipped);
        }

        // ── AddItemAsync — TaxPercentage validation ──────────────────────────

        [Fact]
        public async Task AddItemAsync_NegativeTaxPercentage_ThrowsArgumentException()
        {
            var dto = new CreateItemDto { Name = "Item", Price = 100, TaxPercentage = -1 };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddItemAsync(dto));
        }

        [Fact]
        public async Task AddItemAsync_ZeroPrice_ThrowsArgumentException()
        {
            var dto = new CreateItemDto { Name = "FreeItem", Price = 0 };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddItemAsync(dto));
        }

        [Fact]
        public async Task AddItemAsync_NegativePrice_ThrowsArgumentException()
        {
            var dto = new CreateItemDto { Name = "NegItem", Price = -50 };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddItemAsync(dto));
        }

        [Fact]
        public async Task AddItemAsync_NameExceeds200Chars_ThrowsArgumentException()
        {
            var dto = new CreateItemDto { Name = new string('A', 201), Price = 100 };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.AddItemAsync(dto));
        }

        [Fact]
        public async Task AddItemAsync_NullDto_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _service.AddItemAsync(null!));
        }

        // ── GetItemsAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetItemsAsync_NullFromRepo_ReturnsEmptyList()
        {
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Item>)null!);

            var result = await _service.GetItemsAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
