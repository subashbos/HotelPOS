using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using Xunit;

namespace HotelPOS.Api.Tests.Unit.Services
{
    public class UnitOfMeasurementServiceTests
    {
        private readonly Mock<IUnitOfMeasurementRepository> _unitRepoMock = new();
        private readonly Mock<IItemRepository> _itemRepoMock = new();
        private readonly UnitOfMeasurementService _service;

        public UnitOfMeasurementServiceTests()
        {
            _service = new UnitOfMeasurementService(_unitRepoMock.Object, _itemRepoMock.Object);
        }

        [Fact]
        public async Task GetUnitsAsync_ReturnsAllUnits()
        {
            var units = new List<UnitOfMeasurement>
            {
                new UnitOfMeasurement { Id = 1, Name = "kg", DisplayOrder = 1 },
                new UnitOfMeasurement { Id = 2, Name = "litre", DisplayOrder = 2 }
            };
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(units);

            var result = await _service.GetUnitsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task AddUnitAsync_ValidUnit_ReturnsId()
        {
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>());
            _unitRepoMock.Setup(r => r.AddAsync(It.IsAny<UnitOfMeasurement>()))
                .ReturnsAsync((UnitOfMeasurement u) => { u.Id = 10; return u; });

            var id = await _service.AddUnitAsync("Piece", 1);

            Assert.Equal(10, id);
            _unitRepoMock.Verify(r => r.AddAsync(It.Is<UnitOfMeasurement>(u => u.Name == "Piece" && u.DisplayOrder == 1)), Times.Once);
        }

        [Fact]
        public async Task AddUnitAsync_EmptyName_ShouldThrowArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddUnitAsync("   ", 1));
        }

        [Fact]
        public async Task AddUnitAsync_DuplicateName_ShouldThrowInvalidOperationException()
        {
            var existing = new List<UnitOfMeasurement> { new UnitOfMeasurement { Id = 1, Name = "kg" } };
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existing);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddUnitAsync("kg", 2));
        }

        [Fact]
        public async Task UpdateUnitAsync_ValidUnit_UpdatesSuccessfully()
        {
            var existing = new UnitOfMeasurement { Id = 1, Name = "kg", DisplayOrder = 1 };
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement> { existing });
            _unitRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);

            await _service.UpdateUnitAsync(1, "Kilogram", 2);

            _unitRepoMock.Verify(r => r.UpdateAsync(It.Is<UnitOfMeasurement>(u => u.Id == 1 && u.Name == "Kilogram" && u.DisplayOrder == 2)), Times.Once);
        }

        [Fact]
        public async Task UpdateUnitAsync_InvalidId_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateUnitAsync(0, "Kg"));
        }

        [Fact]
        public async Task UpdateUnitAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasurement>());
            _unitRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UnitOfMeasurement?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateUnitAsync(99, "Kg"));
        }

        [Fact]
        public async Task UpdateUnitAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            var units = new List<UnitOfMeasurement>
            {
                new UnitOfMeasurement { Id = 1, Name = "kg" },
                new UnitOfMeasurement { Id = 2, Name = "litre" }
            };
            _unitRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(units);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateUnitAsync(1, "litre"));
        }

        [Fact]
        public async Task DeleteUnitAsync_WithItems_ThrowsInvalidOperationException()
        {
            var items = new List<Item> { new Item { Id = 1, Name = "Rice", UnitId = 5 } };
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(items);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteUnitAsync(5));
        }

        [Fact]
        public async Task DeleteUnitAsync_Unused_DeletesSuccessfully()
        {
            _itemRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Item>());

            await _service.DeleteUnitAsync(1);

            _unitRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}
