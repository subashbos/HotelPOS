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

        [Fact]
        public void SupplierEntryViewModel_ValidateName_ValidAndEmpty_SetsErrors()
        {
            // Arrange
            var vm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);

            // Act & Assert (empty name)
            vm.Name = "  ";
            Assert.False(vm.ValidateName());
            Assert.True(vm.IsNameInvalid);
            Assert.Equal("Supplier Name is required", vm.NameError);

            // Act & Assert (valid name)
            vm.Name = "Valid Supplier";
            Assert.True(vm.ValidateName());
            Assert.False(vm.IsNameInvalid);
            Assert.Empty(vm.NameError);
        }

        [Fact]
        public void SupplierEntryViewModel_ValidatePhone_ValidInvalidAndEmpty_SetsErrors()
        {
            // Arrange
            var vm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);

            // Act & Assert (empty phone - allowed)
            vm.Phone = "  ";
            Assert.True(vm.ValidatePhone());
            Assert.False(vm.IsPhoneInvalid);
            Assert.Empty(vm.PhoneError);

            // Act & Assert (too short)
            vm.Phone = "1234567";
            Assert.False(vm.ValidatePhone());
            Assert.True(vm.IsPhoneInvalid);
            Assert.Equal("Invalid phone number (must be 10-15 digits)", vm.PhoneError);

            // Act & Assert (valid phone)
            vm.Phone = "+91 98765-43210";
            Assert.True(vm.ValidatePhone());
            Assert.False(vm.IsPhoneInvalid);
            Assert.Empty(vm.PhoneError);
        }

        [Fact]
        public void SupplierEntryViewModel_ValidateEmail_ValidInvalidAndEmpty_SetsErrors()
        {
            // Arrange
            var vm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);

            // Act & Assert (empty email - allowed)
            vm.Email = " ";
            Assert.True(vm.ValidateEmail());
            Assert.False(vm.IsEmailInvalid);
            Assert.Empty(vm.EmailError);

            // Act & Assert (invalid format)
            vm.Email = "plainaddress";
            Assert.False(vm.ValidateEmail());
            Assert.True(vm.IsEmailInvalid);
            Assert.Equal("Please enter a valid Email ID", vm.EmailError);

            // Act & Assert (valid email)
            vm.Email = "info@supplier.co.in";
            Assert.True(vm.ValidateEmail());
            Assert.False(vm.IsEmailInvalid);
            Assert.Empty(vm.EmailError);
        }

        [Fact]
        public void SupplierEntryViewModel_LoadSupplier_MapsAllFieldsAndSetsEditMode()
        {
            // Arrange
            var vm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);
            var supplier = new Supplier
            {
                Id = 42,
                Name = "Apex Food Labs",
                ContactPerson = "John Doe",
                Phone = "9876543210",
                Email = "john@apexfoods.com",
                Gstin = "27AABCU9603R1ZX",
                Address = "123 Street",
                City = "Pune",
                State = "Maharashtra",
                Pincode = "411001",
                OpeningBalance = 1500m,
                CreditLimit = 75000m,
                PaymentTerms = "Credit 30 Days"
            };

            // Act
            vm.LoadSupplier(supplier);

            // Assert
            Assert.Equal(42, vm.Id);
            Assert.Equal("Apex Food Labs", vm.Name);
            Assert.Equal("John Doe", vm.ContactPerson);
            Assert.Equal("9876543210", vm.Phone);
            Assert.Equal("john@apexfoods.com", vm.Email);
            Assert.Equal("27AABCU9603R1ZX", vm.Gstin);
            Assert.Equal("123 Street", vm.Address);
            Assert.Equal("Pune", vm.City);
            Assert.Equal("Maharashtra", vm.State);
            Assert.Equal("411001", vm.Pincode);
            Assert.Equal(1500m, vm.OpeningBalance);
            Assert.Equal(75000m, vm.CreditLimit);
            Assert.Equal("Credit 30 Days", vm.PaymentTerms);
            Assert.True(vm.IsEditMode);

            // Verify errors are cleared
            Assert.Empty(vm.NameError);
            Assert.Empty(vm.PhoneError);
            Assert.Empty(vm.EmailError);

            // Verify with new supplier (Id = 0)
            var newSupplier = new Supplier { Id = 0, Name = "Fresh Supply" };
            vm.LoadSupplier(newSupplier);
            Assert.False(vm.IsEditMode);
        }

        [Fact]
        public void SupplierEntryViewModel_CancelCommand_TriggersRequestCloseWithFalse()
        {
            // Arrange
            var vm = new SupplierEntryViewModel(_supplierService, _notificationServiceMock.Object);
            bool closeFired = false;
            bool successResult = true;

            vm.RequestClose += (sender, result) =>
            {
                closeFired = true;
                successResult = result;
            };

            // Act
            vm.CancelCommand.Execute(null);

            // Assert
            Assert.True(closeFired);
            Assert.False(successResult);
        }

        #endregion
    }
}
