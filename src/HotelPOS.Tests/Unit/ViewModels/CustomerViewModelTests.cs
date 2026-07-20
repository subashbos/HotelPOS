using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class CustomerViewModelTests
    {
        private readonly Mock<ICustomerService> _customerServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly CustomerViewModel _vm;

        public CustomerViewModelTests()
        {
            _vm = new CustomerViewModel(_customerServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task LoadCustomersAsync_PopulatesAndFiltersList()
        {
            var customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "Asha Rao", Phone = "9876543210", Email = "asha@example.com" },
                new Customer { Id = 2, Name = "Vikram Singh", Phone = "8888888888", Gstin = "27AAAAA1111A1Z1" },
                new Customer { Id = 3, Name = "Priya Menon", Phone = "7777777777" }
            };
            _customerServiceMock.Setup(s => s.GetCustomersAsync(false)).ReturnsAsync(customers);

            await _vm.LoadCustomersAsync();
            Assert.Equal(3, _vm.Customers.Count);

            _vm.SearchText = "vikram";
            Assert.Single(_vm.Customers);
            Assert.Equal("Vikram Singh", _vm.Customers[0].Name);

            _vm.SearchText = "7777";
            Assert.Single(_vm.Customers);
            Assert.Equal("Priya Menon", _vm.Customers[0].Name);

            _vm.SearchText = "27AAAAA1111A1Z1";
            Assert.Single(_vm.Customers);
            Assert.Equal("Vikram Singh", _vm.Customers[0].Name);

            _vm.SearchText = "";
            Assert.Equal(3, _vm.Customers.Count);
        }

        [Fact]
        public async Task LoadCustomersAsync_ServiceThrows_ShowsError()
        {
            _customerServiceMock.Setup(s => s.GetCustomersAsync(false)).ThrowsAsync(new Exception("DB down"));

            await _vm.LoadCustomersAsync();

            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(m => m.Contains("Failed to load customers"))), Times.Once);
        }

        [Fact]
        public async Task AddCustomerAsync_DialogReturnsTrue_ReloadsCustomers()
        {
            _customerServiceMock.Setup(s => s.GetCustomersAsync(false)).ReturnsAsync(new List<Customer>());
            bool? passedCustomer = null;
            _vm.ShowEntryDialogAsync = c => { passedCustomer = c == null; return Task.FromResult(true); };

            await _vm.AddCustomerCommand.ExecuteAsync(null);

            Assert.True(passedCustomer);
            _customerServiceMock.Verify(s => s.GetCustomersAsync(false), Times.Once);
        }

        [Fact]
        public async Task AddCustomerAsync_DialogReturnsFalse_DoesNotReload()
        {
            _vm.ShowEntryDialogAsync = _ => Task.FromResult(false);

            await _vm.AddCustomerCommand.ExecuteAsync(null);

            _customerServiceMock.Verify(s => s.GetCustomersAsync(It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task EditCustomerAsync_NoTargetSelected_ShowsWarning()
        {
            _vm.SelectedCustomer = null;

            await _vm.EditCustomerCommand.ExecuteAsync(null);

            _notificationServiceMock.Verify(n => n.ShowWarning("Please select a customer to edit."), Times.Once);
        }

        [Fact]
        public async Task EditCustomerAsync_UsesSelectedCustomerWhenParamNull()
        {
            var customer = new Customer { Id = 5, Name = "Selected" };
            _vm.SelectedCustomer = customer;
            _customerServiceMock.Setup(s => s.GetCustomersAsync(false)).ReturnsAsync(new List<Customer>());

            Customer? received = null;
            _vm.ShowEntryDialogAsync = c => { received = c; return Task.FromResult(true); };

            await _vm.EditCustomerCommand.ExecuteAsync(null);

            Assert.Same(customer, received);
        }

        [Fact]
        public async Task ViewHistoryAsync_NoTargetSelected_ShowsWarning()
        {
            _vm.SelectedCustomer = null;

            await _vm.ViewHistoryCommand.ExecuteAsync(null);

            _notificationServiceMock.Verify(n => n.ShowWarning("Please select a customer to view history."), Times.Once);
        }

        [Fact]
        public async Task ViewHistoryAsync_InvokesCallbackWithTarget()
        {
            var customer = new Customer { Id = 9, Name = "History Target" };
            Customer? received = null;
            _vm.ShowHistoryAsync = c => { received = c; return Task.CompletedTask; };

            await _vm.ViewHistoryCommand.ExecuteAsync(customer);

            Assert.Same(customer, received);
        }

        [Fact]
        public async Task DeleteCustomerAsync_NoTargetSelected_ShowsWarning()
        {
            _vm.SelectedCustomer = null;

            await _vm.DeleteCustomerCommand.ExecuteAsync(null);

            _notificationServiceMock.Verify(n => n.ShowWarning("Please select a customer to deactivate."), Times.Once);
        }

        [Fact]
        public async Task DeleteCustomerAsync_Confirmed_DeactivatesAndReloads()
        {
            var customer = new Customer { Id = 10, Name = "To Deactivate" };
            _vm.SelectedCustomer = customer;
            _vm.ConfirmDeactivateAsync = _ => Task.FromResult(true);
            _customerServiceMock.Setup(s => s.GetCustomersAsync(false)).ReturnsAsync(new List<Customer>());

            await _vm.DeleteCustomerCommand.ExecuteAsync(null);

            _customerServiceMock.Verify(s => s.DeleteCustomerAsync(10), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess("Customer 'To Deactivate' deactivated successfully."), Times.Once);
        }

        [Fact]
        public async Task DeleteCustomerAsync_Cancelled_DoesNotDeactivate()
        {
            var customer = new Customer { Id = 10, Name = "To Deactivate" };
            _vm.SelectedCustomer = customer;
            _vm.ConfirmDeactivateAsync = _ => Task.FromResult(false);

            await _vm.DeleteCustomerCommand.ExecuteAsync(null);

            _customerServiceMock.Verify(s => s.DeleteCustomerAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCustomerAsync_OnException_ShowsError()
        {
            var customer = new Customer { Id = 10, Name = "To Deactivate" };
            _vm.SelectedCustomer = customer;
            _vm.ConfirmDeactivateAsync = _ => Task.FromResult(true);
            _customerServiceMock.Setup(s => s.DeleteCustomerAsync(10)).ThrowsAsync(new Exception("Database error"));

            await _vm.DeleteCustomerCommand.ExecuteAsync(null);

            _notificationServiceMock.Verify(n => n.ShowError(It.Is<string>(m => m.Contains("Failed to deactivate customer") && m.Contains("Database error"))), Times.Once);
        }
    }
}
