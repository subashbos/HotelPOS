using System;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class CustomerEntryViewModelTests
    {
        private readonly Mock<ICustomerService> _mockCustomerService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly CustomerEntryViewModel _vm;

        public CustomerEntryViewModelTests()
        {
            _vm = new CustomerEntryViewModel(_mockCustomerService.Object, _mockNotif.Object);
        }

        [Fact]
        public void LoadCustomer_PopulatesProperties()
        {
            var customer = new Customer
            {
                Id = 42,
                Name = "Test Customer",
                Phone = "1234567890",
                Email = "test@customer.com",
                Gstin = "GST123",
                Address = "123 Street",
                Notes = "Prefers window seating"
            };

            _vm.LoadCustomer(customer);

            Assert.Equal(42, _vm.Id);
            Assert.Equal("Test Customer", _vm.Name);
            Assert.Equal("1234567890", _vm.Phone);
            Assert.Equal("test@customer.com", _vm.Email);
            Assert.Equal("GST123", _vm.Gstin);
            Assert.Equal("123 Street", _vm.Address);
            Assert.Equal("Prefers window seating", _vm.Notes);
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
        [InlineData(null, true)] // Allowed to be empty
        [InlineData("12345", false)] // Too short
        [InlineData("1234567890", true)] // Valid 10 digit
        [InlineData("123456789012345", true)] // Valid 15 digit
        [InlineData("1234567890123456", false)] // Too long
        public void ValidatePhone_ChecksLengthRange(string? phone, bool expectedResult)
        {
            _vm.Phone = phone;
            var result = _vm.ValidatePhone();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsPhoneInvalid);
        }

        [Theory]
        [InlineData(null, true)] // Allowed to be empty
        [InlineData("invalid-email", false)]
        [InlineData("test@domain.com", true)]
        public void ValidateEmail_ChecksPattern(string? email, bool expectedResult)
        {
            _vm.Email = email;
            var result = _vm.ValidateEmail();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsEmailInvalid);
        }

        [Fact]
        public void Cancel_RaisesRequestCloseWithFalse()
        {
            bool? closedResult = null;
            _vm.RequestClose += (sender, result) => closedResult = result;

            _vm.CancelCommand.Execute(null);

            Assert.NotNull(closedResult);
            Assert.False(closedResult.Value);
        }

        [Fact]
        public async Task SaveCommand_InvalidForm_ShowsWarning()
        {
            _vm.Name = ""; // Invalid

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockCustomerService.Verify(s => s.SaveCustomerAsync(It.IsAny<Customer>()), Times.Never);
        }
    }
}
