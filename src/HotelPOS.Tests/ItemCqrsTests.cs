using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases.Items.Commands;
using HotelPOS.Application.UseCases.Items.Queries;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class ItemCqrsTests
    {
        private readonly Mock<IItemRepository> _repoMock;

        public ItemCqrsTests()
        {
            _repoMock = new Mock<IItemRepository>();
        }

        [Fact]
        public async Task GetItemsQuery_ShouldReturnAllItems()
        {
            // Arrange
            var items = new List<Item> { new Item { Id = 1, Name = "Item1" }, new Item { Id = 2, Name = "Item2" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);
            var query = new GetItemsQuery();
            var handler = new GetItemsQueryHandler(_repoMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Item1", result[0].Name);
        }

        [Fact]
        public async Task GetItemByIdQuery_ShouldReturnItem_WhenExists()
        {
            // Arrange
            var item = new Item { Id = 1, Name = "Item1" };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
            var query = new GetItemByIdQuery(1);
            var handler = new GetItemByIdQueryHandler(_repoMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Item1", result.Name);
        }

        [Fact]
        public async Task GetItemByIdQuery_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Item?)null);
            var query = new GetItemByIdQuery(99);
            var handler = new GetItemByIdQueryHandler(_repoMock.Object);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateItemCommand_ShouldAddItem_WhenValid()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());
            var command = new CreateItemCommand(
                Name: "Pizza",
                Price: 250,
                TaxPercentage: 12,
                CategoryId: 2,
                HsnCode: "HSN123",
                Barcode: "BAR123",
                StockQuantity: 100,
                TrackInventory: true
            );
            var handler = new CreateItemCommandHandler(_repoMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pizza", result.Name);
            Assert.Equal(250, result.Price);
            _repoMock.Verify(r => r.AddAsync(It.Is<Item>(i => i.Name == "Pizza" && i.Barcode == "BAR123")), Times.Once);
        }

        [Fact]
        public async Task CreateItemCommand_DuplicateName_ShouldThrow()
        {
            // Arrange
            var existing = new List<Item> { new Item { Name = "Pizza" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
            var command = new CreateItemCommand(
                Name: "Pizza",
                Price: 250,
                TaxPercentage: 12,
                CategoryId: 2,
                HsnCode: "HSN123",
                Barcode: "BAR123",
                StockQuantity: 100,
                TrackInventory: true
            );
            var handler = new CreateItemCommandHandler(_repoMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        }
    }
}
