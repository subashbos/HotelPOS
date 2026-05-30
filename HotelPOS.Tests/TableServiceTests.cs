using HotelPOS.Application.DTOs.Table;
using HotelPOS.Application;
using HotelPOS.Domain;
using HotelPOS.Domain.Interfaces;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class TableServiceTests
    {
        private readonly Mock<ITableRepository> _repoMock;
        private readonly TableService _service;

        public TableServiceTests()
        {
            _repoMock = new Mock<ITableRepository>();
            _service = new TableService(_repoMock.Object);
        }

        [Fact]
        public async Task AddTableAsync_ValidDto_ShouldAdd()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Table>());
            var dto = new CreateTableDto { Number = 1, Name = "T1", Capacity = 4, IsActive = true };

            // Act
            await _service.AddTableAsync(dto);

            // Assert
            _repoMock.Verify(r => r.AddAsync(It.Is<Table>(t => t.Number == 1 && t.Name == "T1")), Times.Once);
        }

        [Fact]
        public async Task AddTableAsync_DuplicateNumber_ShouldThrow()
        {
            // Arrange
            var existing = new List<Table> { new Table { Id = 1, Number = 5, IsDeleted = false } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
            var dto = new CreateTableDto { Number = 5, Name = "New T5" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddTableAsync(dto));
        }

        [Fact]
        public async Task AddTableAsync_NegativeNumber_ShouldThrow()
        {
            // Arrange
            var dto = new CreateTableDto { Number = -1, Name = "Invalid" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTableAsync(dto));
        }

        [Fact]
        public async Task UpdateTableAsync_DuplicateNumber_ShouldThrow()
        {
            // Arrange
            var existing = new List<Table> 
            { 
                new Table { Id = 1, Number = 5 }, 
                new Table { Id = 2, Number = 10 } 
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
            var dto = new CreateTableDto { Number = 10, Name = "Rename 5 to 10" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateTableAsync(1, dto));
        }

        [Fact]
        public async Task UpdateTableAsync_SameNumber_ShouldAllow()
        {
            // Arrange
            var table = new Table { Id = 1, Number = 5, Name = "Old" };
            var existing = new List<Table> { table };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(table);
            
            var dto = new CreateTableDto { Number = 5, Name = "New Name" };

            // Act
            await _service.UpdateTableAsync(1, dto);

            // Assert
            Assert.Equal("New Name", table.Name);
            _repoMock.Verify(r => r.UpdateAsync(table), Times.Once);
        }

        [Fact]
        public async Task GetTablesAsync_ShouldReturnList()
        {
            // Arrange
            var tables = new List<Table> { new Table { Name = "T1" }, new Table { Name = "T2" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tables);

            // Act
            var result = await _service.GetTablesAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteTableAsync_ShouldCallRepo()
        {
            // Act
            await _service.DeleteTableAsync(1);

            // Assert
            _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
