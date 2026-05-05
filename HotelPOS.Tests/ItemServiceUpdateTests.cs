using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Tests for ItemService.UpdateItemAsync — previously untested.
    /// </summary>
    public class ItemServiceUpdateTests
    {
        private readonly Mock<IItemRepository> _repoMock = new();
        private readonly ItemService _service;

        public ItemServiceUpdateTests()
        {
            _service = new ItemService(_repoMock.Object);
        }

        // ========== UpdateItemAsync — happy path ===========

        [Fact]
        public async Task UpdateItemAsync_ValidDto_UpdatesAllFields()
        {
            // Arrange
            var existing = new Item { Id = 1, Name = "Old Name", Price = 50, TaxPercentage = 5 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            var dto = new CreateItemDto
            {
                Name = "  New Burger  ",
                Price = 150,
                TaxPercentage = 12,
                HsnCode = "2106",
                CategoryId = 3,
                StockQuantity = 20,
                TrackInventory = true,
                Barcode = "BAR123"
            };

            // Act
            await _service.UpdateItemAsync(1, dto);

            // Assert — all fields mapped correctly (name trimmed)
            Assert.Equal("New Burger", existing.Name);
            Assert.Equal(150, existing.Price);
            Assert.Equal(12, existing.TaxPercentage);
            Assert.Equal("2106", existing.HsnCode);
            Assert.Equal(3, existing.CategoryId);
            Assert.Equal(20, existing.StockQuantity);
            Assert.True(existing.TrackInventory);
            Assert.Equal("BAR123", existing.Barcode);
            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_NameWithWhitespace_TrimsName()
        {
            var existing = new Item { Id = 2, Name = "Old", Price = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(existing);

            await _service.UpdateItemAsync(2, new CreateItemDto { Name = "  Trimmed  ", Price = 10 });

            Assert.Equal("Trimmed", existing.Name);
            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_ZeroTax_AllowsUpdate()
        {
            var existing = new Item { Id = 3, Name = "Item", Price = 100, TaxPercentage = 18 };
            _repoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);

            await _service.UpdateItemAsync(3, new CreateItemDto { Name = "Item", Price = 100, TaxPercentage = 0 });

            Assert.Equal(0, existing.TaxPercentage);
            _repoMock.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        // ========== UpdateItemAsync — not found ===========

        [Fact]
        public async Task UpdateItemAsync_ItemNotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Item?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateItemAsync(999, new CreateItemDto { Name = "X", Price = 10 }));

            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        // ========== UpdateItemAsync — stock tracking toggle ===========

        [Fact]
        public async Task UpdateItemAsync_EnableTrackInventory_SetsFlag()
        {
            var existing = new Item { Id = 4, Name = "Juice", Price = 30, TrackInventory = false };
            _repoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(existing);

            await _service.UpdateItemAsync(4, new CreateItemDto
            {
                Name = "Juice",
                Price = 30,
                TrackInventory = true,
                StockQuantity = 50
            });

            Assert.True(existing.TrackInventory);
            Assert.Equal(50, existing.StockQuantity);
        }

        [Fact]
        public async Task UpdateItemAsync_DisableTrackInventory_SetsFlag()
        {
            var existing = new Item { Id = 5, Name = "Snack", Price = 20, TrackInventory = true, StockQuantity = 10 };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);

            await _service.UpdateItemAsync(5, new CreateItemDto
            {
                Name = "Snack",
                Price = 20,
                TrackInventory = false,
                StockQuantity = 0
            });

            Assert.False(existing.TrackInventory);
        }

        // ========== DeductStockAsync — update path (negative qty = return stock) ===========

        [Fact]
        public async Task DeductStockAsync_NegativeQuantity_IncreasesStock()
        {
            var item = new Item { Id = 10, Name = "Cola", StockQuantity = 5, TrackInventory = true };
            _repoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(item);

            // Simulate returning 3 units (order edit/delete flow)
            await _service.DeductStockAsync(10, -3);

            Assert.Equal(8, item.StockQuantity);
            _repoMock.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task DeductStockAsync_WhenTrackInventoryFalse_DoesNotUpdateStock()
        {
            var item = new Item { Id = 11, Name = "Water", StockQuantity = 100, TrackInventory = false };
            _repoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(item);

            await _service.DeductStockAsync(11, 10);

            Assert.Equal(100, item.StockQuantity); // unchanged
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task DeductStockAsync_ItemNotFound_DoesNotThrow()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Item?)null);

            var ex = await Record.ExceptionAsync(() => _service.DeductStockAsync(999, 5));
            Assert.Null(ex);
            _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }
    }
}
