using HotelPOS.Application;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests
{
    public class SupplierTests
    {
        private readonly Mock<ISupplierRepository> _supplierRepoMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly SupplierService _supplierService;

        public SupplierTests()
        {
            _supplierRepoMock = new Mock<ISupplierRepository>();
            _notificationServiceMock = new Mock<INotificationService>();
            _supplierService = new SupplierService(_supplierRepoMock.Object);
        }

        #region Service Layer Tests (SupplierService)

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

        #endregion

        #region ViewModel Layer Tests (SupplierViewModel & SupplierEntryViewModel)

        [Fact]
        public async Task SupplierViewModel_FiltersListCorrectly()
        {
            // Arrange
            var mockSuppliers = new List<Supplier>
            {
                new Supplier { Id = 1, Name = "Metro Wholesalers", City = "Mumbai", Phone = "9876543210" },
                new Supplier { Id = 2, Name = "Apex Food Distributors", City = "Pune", Phone = "8888888888" },
                new Supplier { Id = 3, Name = "Supreme Dairy Partners", City = "Mumbai", Phone = "7777777777" }
            };

            _supplierRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(mockSuppliers);

            var vm = new SupplierViewModel(_supplierService, _notificationServiceMock.Object);

            // Act & Assert (Load initially)
            await vm.LoadSuppliersAsync();
            Assert.Equal(3, vm.Suppliers.Count);

            // Filter by Name
            vm.SearchText = "Apex";
            Assert.Single(vm.Suppliers);
            Assert.Equal("Apex Food Distributors", vm.Suppliers[0].Name);

            // Filter by City
            vm.SearchText = "Mumbai";
            Assert.Equal(2, vm.Suppliers.Count);

            // Filter by Phone
            vm.SearchText = "7777";
            Assert.Single(vm.Suppliers);
            Assert.Equal("Supreme Dairy Partners", vm.Suppliers[0].Name);
        }

        [Fact]
        public async Task SupplierEntryViewModel_SaveCommand_ValidatesAndTriggersSave()
        {
            // Arrange
            var entryVm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);
            entryVm.Name = "New Vendor";
            entryVm.Phone = "9876543210";
            entryVm.Email = "vendor@email.com";
            entryVm.City = "Nashik";

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("New Vendor", 0)).ReturnsAsync(false);

            bool closeRequested = false;
            entryVm.RequestClose += (s, success) => closeRequested = success;

            // Act
            await entryVm.SaveCommand.ExecuteAsync(null);

            // Assert
            _supplierRepoMock.Verify(r => r.AddAsync(It.Is<Supplier>(s => s.Name == "New Vendor")), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.IsAny<string>()), Times.Once);
            Assert.True(closeRequested);
        }

        [Fact]
        public async Task SupplierEntryViewModel_SaveCommand_DuplicateCheckPreventsSaving()
        {
            // Arrange
            var entryVm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);
            entryVm.Name = "Duplicate Name";
            entryVm.Phone = "9876543210";

            _supplierRepoMock.Setup(r => r.ExistsByNameAsync("Duplicate Name", 0)).ReturnsAsync(true);

            // Act
            await entryVm.SaveCommand.ExecuteAsync(null);

            // Assert
            _supplierRepoMock.Verify(r => r.AddAsync(It.IsAny<Supplier>()), Times.Never);
            _notificationServiceMock.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("already exists"))), Times.Once);
        }

        #endregion
    }
}
