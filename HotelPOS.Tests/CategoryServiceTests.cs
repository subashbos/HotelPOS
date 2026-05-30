using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _catRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _service = new CategoryService(_catRepoMock.Object, _itemRepoMock.Object);
        }

        [Fact]
        public async Task AddCategoryAsync_DuplicateName_ShouldThrow()
        {
            // Arrange
            var existing = new List<Category> { new Category { Name = "Food" } };
            _catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddCategoryAsync("Food"));
        }

        [Fact]
        public async Task DeleteCategoryAsync_WithItems_ShouldThrow()
        {
            // Arrange
            var items = new List<Item> { new Item { CategoryId = 5, Name = "Item1" } };
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteCategoryAsync(5));
        }

        [Fact]
        public async Task DeleteCategoryAsync_Empty_ShouldDelete()
        {
            // Arrange
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

            // Act
            await _service.DeleteCategoryAsync(1);

            // Assert
            _catRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
