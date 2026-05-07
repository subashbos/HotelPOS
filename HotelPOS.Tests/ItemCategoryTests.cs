using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interface;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ItemCategoryTests
    {
        [Fact]
        public async Task ItemService_AddItem_SavesCategoryId()
        {
            // Arrange
            var mockRepo = new Mock<IItemRepository>();
            var service = new ItemService(mockRepo.Object);
            var dto = new CreateItemDto
            {
                Name = "Test Item",
                Price = 100,
                CategoryId = 5,
                TaxPercentage = 5
            };

            // Act
            await service.AddItemAsync(dto);

            // Assert
            mockRepo.Verify(r => r.AddAsync(It.Is<Item>(i => i.CategoryId == 5)), Times.Once);
        }

        [Fact]
        public async Task CategoryService_AddCategory_Works()
        {
            // Arrange
            var mockRepo = new Mock<ICategoryRepository>();
            var service = new CategoryService(mockRepo.Object);

            // Act
            await service.AddCategoryAsync("New Category");

            // Assert
            mockRepo.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "New Category")), Times.Once);
        }

        [Fact]
        public async Task CategoryService_DeleteCategory_Works()
        {
            // Arrange
            var mockRepo = new Mock<ICategoryRepository>();
            var service = new CategoryService(mockRepo.Object);

            // Act
            await service.DeleteCategoryAsync(10);

            // Assert
            mockRepo.Verify(r => r.DeleteAsync(10), Times.Once);
        }
    }
}
