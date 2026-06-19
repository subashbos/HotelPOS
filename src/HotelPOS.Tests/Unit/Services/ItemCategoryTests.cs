using HotelPOS.Application.DTOs.Item;
using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
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
            mockRepo.Setup(r => r.AddAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => { c.Id = 1; return c; });
            var itemRepo = new Mock<IItemRepository>();
            var service = new CategoryService(mockRepo.Object, itemRepo.Object);

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
            var itemRepo = new Mock<IItemRepository>();
            itemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            var service = new CategoryService(mockRepo.Object, itemRepo.Object);

            // Act
            await service.DeleteCategoryAsync(10);

            // Assert
            mockRepo.Verify(r => r.DeleteAsync(10), Times.Once);
        }
    }
}
