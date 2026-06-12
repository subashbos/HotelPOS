using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class SupplierServiceTests
    {
        private readonly Mock<ISupplierRepository> _supplierRepoMock;
        private readonly SupplierService _supplierService;

        public SupplierServiceTests()
        {
            _supplierRepoMock = new Mock<ISupplierRepository>();
            _supplierService = new SupplierService(_supplierRepoMock.Object);
        }

        [Fact]
        public async Task SaveSupplierAsync_ValidNewSupplier_ShouldSaveSuccessfully()
        {
            // Arrange
            var supplier = new Supplier
            {
                Id = 0,
                Name = "Alpha Traders",
                Phone = "9876543210",
                Email = "alpha@traders.com",
                City = "Mumbai"
            };

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("Alpha Traders", 0)).ReturnsAsync(false);

            // Act
            await _supplierService.SaveSupplierAsync(supplier);

            // Assert
            _supplierRepoMock.Verify(r => r.AddAsync(supplier), Times.Once);
            Assert.Equal("Alpha Traders", supplier.Name);
            Assert.Equal("9876543210", supplier.Phone);
        }

        [Fact]
        public async Task SaveSupplierAsync_EmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "",
                Phone = "9876543210"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.SaveSupplierAsync(supplier));
            Assert.Contains("Supplier Name is required.", ex.Message);
        }

        [Fact]
        public async Task SaveSupplierAsync_EmptyPhone_ShouldSaveSuccessfully()
        {
            // Arrange
            var supplier = new Supplier
            {
                Id = 0,
                Name = "Beta Distributors",
                Phone = "   "
            };

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("Beta Distributors", 0)).ReturnsAsync(false);

            // Act
            await _supplierService.SaveSupplierAsync(supplier);

            // Assert
            _supplierRepoMock.Verify(r => r.AddAsync(supplier), Times.Once);
            Assert.Equal(string.Empty, supplier.Phone);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("123456789")]
        [InlineData("1234567890123456")]
        public async Task SaveSupplierAsync_InvalidPhoneLength_ShouldThrowArgumentException(string invalidPhone)
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Beta Distributors",
                Phone = invalidPhone
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.SaveSupplierAsync(supplier));
            Assert.Contains("Phone number must be a valid number between 10 and 15 digits.", ex.Message);
        }

        [Fact]
        public async Task SaveSupplierAsync_InvalidEmail_ShouldThrowArgumentException()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Beta Distributors",
                Phone = "9876543210",
                Email = "invalid-email"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.SaveSupplierAsync(supplier));
            Assert.Contains("Email ID is invalid.", ex.Message);
        }

        [Fact]
        public async Task SaveSupplierAsync_DuplicateName_ShouldThrowArgumentException()
        {
            // Arrange
            var supplier = new Supplier
            {
                Id = 0,
                Name = "Metro Wholesalers",
                Phone = "9876543210"
            };

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("Metro Wholesalers", 0)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _supplierService.SaveSupplierAsync(supplier));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task SaveSupplierAsync_InvalidGstinFormat_ThrowsArgumentException()
        {
            // Arrange
            var supplier = new Supplier
            {
                Name = "Beta Distributors",
                Gstin = "INVALID_GSTIN!!!"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => _supplierService.SaveSupplierAsync(supplier));
            Assert.Contains("GSTIN format is invalid", ex.Message);
        }

        [Fact]
        public async Task SaveSupplierAsync_ValidGstinFormat_Succeeds()
        {
            // Arrange
            var supplier = new Supplier
            {
                Id = 0,
                Name = "Gamma Logistics",
                Gstin = "27AABCU9603R1ZX" // Valid Indian GSTIN format
            };

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("Gamma Logistics", 0)).ReturnsAsync(false);

            // Act
            await _supplierService.SaveSupplierAsync(supplier);

            // Assert
            _supplierRepoMock.Verify(r => r.AddAsync(supplier), Times.Once);
            Assert.Equal("27AABCU9603R1ZX", supplier.Gstin);
        }

        [Fact]
        public async Task GetSuppliersAsync_NullFromRepo_ReturnsEmptyList()
        {
            // Arrange
            _supplierRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Supplier>)null!);

            // Act
            var result = await _supplierService.GetSuppliersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteSupplierAsync_NotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _supplierRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Supplier?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _supplierService.DeleteSupplierAsync(999));
        }
    }
}
