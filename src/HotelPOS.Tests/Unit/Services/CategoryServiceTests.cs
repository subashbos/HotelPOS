using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
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

        [Fact]
        public async Task UpdateCategoryAsync_NotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());
            _catRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateCategoryAsync(99, "Dessert"));
        }

        [Fact]
        public async Task UpdateCategoryAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var existing = new List<Category>
            {
                new Category { Id = 1, Name = "Food" },
                new Category { Id = 2, Name = "Drinks" }
            };
            _catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateCategoryAsync(1, "Drinks"));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task UpdateCategoryAsync_EmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateCategoryAsync(1, "   "));
        }

        [Fact]
        public async Task GetCategoriesAsync_NullFromRepo_ReturnsEmptyList()
        {
            // Arrange
            _catRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Category>)null!);

            // Act
            var result = await _service.GetCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}

