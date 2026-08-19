using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Covers ItemService edge cases missing from ItemServiceTests.cs:
    /// TrackInventory=false stock return, insufficient stock, DeleteItem invalid id,
    /// BulkAdd empty list, and TaxPercentage validation. DeductStock/UpdateItem
    /// not-found and stock-return paths live in ItemServiceUpdateTests.
    /// </summary>
    public class ItemServiceLoopholeTests
    {
        private readonly Mock<IItemRepository> _repo = new();
        private readonly ItemService _service;

        public ItemServiceLoopholeTests()
        {
            _service = new ItemService(_repo.Object);
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
            _repo.Setup(r => r.TryDeductStockAsync(5, 5)).ReturnsAsync(true);

            var ex = await Record.ExceptionAsync(() => _service.DeductStockAsync(5, 5));

            Assert.Null(ex);
            _repo.Verify(r => r.TryDeductStockAsync(5, 5), Times.Once);
        }

        [Fact]
        public async Task DeductStockAsync_ConcurrentOversell_SecondCallerFails()
        {
            // Regression guard for the stock-deduction race: even though the initial GetByIdAsync
            // read here sees sufficient stock, the atomic TryDeductStockAsync is the sole source of
            // truth for whether the deduction actually succeeded — simulating a concurrent caller
            // having already taken the last unit between the read and the guarded UPDATE.
            var item = new Item { Id = 6, Name = "LastUnit", StockQuantity = 1, TrackInventory = true };
            _repo.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(item);
            _repo.Setup(r => r.TryDeductStockAsync(6, 1)).ReturnsAsync(false);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeductStockAsync(6, 1));
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

