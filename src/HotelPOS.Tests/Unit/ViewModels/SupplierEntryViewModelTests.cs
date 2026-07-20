using System;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class SupplierEntryViewModelTests
    {
        private readonly Mock<ISupplierService> _mockSupplierService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly SupplierEntryViewModel _vm;

        public SupplierEntryViewModelTests()
        {
            _vm = new SupplierEntryViewModel(_mockSupplierService.Object, _mockNotif.Object);
        }

        [Fact]
        public void LoadSupplier_PopulatesProperties()
        {
            // Arrange
            var supplier = new Supplier
            {
                Id = 42,
                Name = "Test Supplier",
                ContactPerson = "John Doe",
                Phone = "1234567890",
                Email = "test@supplier.com",
                Gstin = "GST123",
                Address = "123 Street",
                City = "CityName",
                State = "StateName",
                Pincode = "123456",
                OpeningBalance = 1000m,
                CreditLimit = 25000m,
                PaymentTerms = "Credit"
            };

            // Act
            _vm.LoadSupplier(supplier);

            // Assert
            Assert.Equal(42, _vm.Id);
            Assert.Equal("Test Supplier", _vm.Name);
            Assert.Equal("John Doe", _vm.ContactPerson);
            Assert.Equal("1234567890", _vm.Phone);
            Assert.Equal("test@supplier.com", _vm.Email);
            Assert.Equal("GST123", _vm.Gstin);
            Assert.Equal("123 Street", _vm.Address);
            Assert.Equal("CityName", _vm.City);
            Assert.Equal("StateName", _vm.State);
            Assert.Equal("123456", _vm.Pincode);
            Assert.Equal(1000m, _vm.OpeningBalance);
            Assert.Equal(25000m, _vm.CreditLimit);
            Assert.Equal("Credit", _vm.PaymentTerms);
            Assert.True(_vm.IsEditMode);
            Assert.False(_vm.IsNameInvalid);
            Assert.False(_vm.IsPhoneInvalid);
            Assert.False(_vm.IsEmailInvalid);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Valid Name", true)]
        public void ValidateName_ChecksForEmpty(string name, bool expectedResult)
        {
            _vm.Name = name;
            var result = _vm.ValidateName();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsNameInvalid);
        }

        [Theory]
        [InlineData("", true)] // Allowed to be empty
        [InlineData("12345", false)] // Too short
        [InlineData("1234567890", true)] // Valid 10 digit
        [InlineData("123456789012345", true)] // Valid 15 digit
        [InlineData("1234567890123456", false)] // Too long
        public void ValidatePhone_ChecksLengthRange(string phone, bool expectedResult)
        {
            _vm.Phone = phone;
            var result = _vm.ValidatePhone();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsPhoneInvalid);
        }

        [Theory]
        [InlineData("", true)] // Allowed to be empty
        [InlineData("invalid-email", false)]
        [InlineData("test@domain.com", true)]
        public void ValidateEmail_ChecksPattern(string email, bool expectedResult)
        {
            _vm.Email = email;
            var result = _vm.ValidateEmail();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsEmailInvalid);
        }

        [Fact]
        public void Cancel_RaisesRequestCloseWithFalse()
        {
            // Arrange
            bool? closedResult = null;
            _vm.RequestClose += (sender, result) => closedResult = result;

            // Act
            _vm.CancelCommand.Execute(null);

            // Assert
            Assert.NotNull(closedResult);
            Assert.False(closedResult.Value);
        }

        [Fact]
        public async Task SaveCommand_InvalidForm_ShowsWarning()
        {
            // Arrange
            _vm.Name = ""; // Invalid

            // Act
            await _vm.SaveCommand.ExecuteAsync(null);

            // Assert
            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockSupplierService.Verify(s => s.SaveSupplierAsync(It.IsAny<Supplier>()), Times.Never);
        }
    }
}

