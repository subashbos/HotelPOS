using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _repoMock;
        private readonly ItemService _service;

        public ItemServiceTests()
        {
            _repoMock = new Mock<IItemRepository>();
            _service = new ItemService(_repoMock.Object);
        }

        [Fact]
        public async Task AddItemAsync_ValidDto_ShouldAdd()
        {
            // Arrange
            var dto = new CreateItemDto { Name = "Burger", Price = 100, TaxPercentage = 5 };

            // Act
            await _service.AddItemAsync(dto);

            // Assert
            _repoMock.Verify(r => r.AddAsync(It.Is<Item>(i => i.Name == "Burger" && i.Price == 100)), Times.Once);
        }

        [Fact]
        public async Task AddItemAsync_InvalidPrice_ShouldThrowException()
        {
            // Arrange
            var dto = new CreateItemDto { Name = "Free Food", Price = 0 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddItemAsync(dto));
        }

        [Fact]
        public async Task DeductStockAsync_WhenTrackInventoryIsTrue_ShouldUpdateStock()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Coke", StockQuantity = 50, TrackInventory = true };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);

            // Act
            await _service.DeductStockAsync(1, 10);

            // Assert
            Assert.Equal(40, item.StockQuantity);
            _repoMock.Verify(r => r.UpdateAsync(item), Times.Once);
        }

        [Fact]
        public async Task BulkAddAsync_ShouldSkipExistingItems()
        {
            // Arrange
            var existing = new List<Item> { new Item { Name = "Pizza" } };
            var dtos = new List<CreateItemDto>
            {
                new CreateItemDto { Name = "Pizza", Price = 200 }, // Skip
                new CreateItemDto { Name = "Pasta", Price = 150 }  // Add
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

            // Act
            var result = await _service.BulkAddAsync(dtos);

            // Assert
            Assert.Equal(1, result.Added);
            Assert.Equal(1, result.Skipped);
            _repoMock.Verify(r => r.AddAsync(It.Is<Item>(i => i.Name == "Pasta")), Times.Once);
        }
    }
}
