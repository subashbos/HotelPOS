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
    public class EmployeeViewModelTests
    {
        private readonly Mock<IEmployeeService> _mockEmployeeService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly EmployeeViewModel _vm;

        public EmployeeViewModelTests()
        {
            _vm = new EmployeeViewModel(_mockEmployeeService.Object, _mockNotif.Object);
        }

        private static List<Employee> SampleEmployees() => new()
        {
            new Employee
            {
                Id = 1, EmployeeCode = "EMP-001", FirstName = "Alice", LastName = "Smith", Phone = "9876543210",
                Department = new Department { Id = 1, Name = "Housekeeping" },
                Designation = new Designation { Id = 1, Title = "Supervisor" }
            },
            new Employee
            {
                Id = 2, EmployeeCode = "EMP-002", FirstName = "Bob", LastName = "Jones", Phone = "8888888888",
                Department = new Department { Id = 2, Name = "Kitchen" },
                Designation = new Designation { Id = 2, Title = "Chef" }
            },
            new Employee
            {
                Id = 3, EmployeeCode = "EMP-003", FirstName = "Carol", LastName = "Smith", Phone = "7777777777",
                Department = new Department { Id = 1, Name = "Housekeeping" },
                Designation = new Designation { Id = 3, Title = "Manager" }
            }
        };

        [Fact]
        public async Task LoadEmployeesAsync_PopulatesEmployees()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());

            await _vm.LoadEmployeesAsync();

            Assert.Equal(3, _vm.Employees.Count);
        }

        [Fact]
        public async Task LoadEmployeesAsync_ServiceThrows_ShowsError()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ThrowsAsync(new InvalidOperationException("load fail"));

            await _vm.LoadEmployeesAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load employees") && s.Contains("load fail"))), Times.Once);
            Assert.Empty(_vm.Employees);
        }

        [Fact]
        public async Task SearchText_FiltersByFirstNameLastNamePhoneCodeDepartmentDesignation()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());
            await _vm.LoadEmployeesAsync();
            Assert.Equal(3, _vm.Employees.Count);

            _vm.SearchText = "Bob";
            Assert.Single(_vm.Employees);
            Assert.Equal("Bob", _vm.Employees[0].FirstName);

            _vm.SearchText = "Smith";
            Assert.Equal(2, _vm.Employees.Count);

            _vm.SearchText = "7777";
            Assert.Single(_vm.Employees);
            Assert.Equal("Carol", _vm.Employees[0].FirstName);

            _vm.SearchText = "EMP-002";
            Assert.Single(_vm.Employees);

            _vm.SearchText = "Kitchen";
            Assert.Single(_vm.Employees);
            Assert.Equal("Bob", _vm.Employees[0].FirstName);

            _vm.SearchText = "Manager";
            Assert.Single(_vm.Employees);
            Assert.Equal("Carol", _vm.Employees[0].FirstName);

            _vm.SearchText = "";
            Assert.Equal(3, _vm.Employees.Count);

            _vm.SearchText = "nonexistent-xyz";
            Assert.Empty(_vm.Employees);
        }

        [Fact]
        public async Task AddEmployeeAsync_NoDialogHandler_DoesNotLoadEmployees()
        {
            _vm.ShowEntryDialogAsync = null;

            await _vm.AddEmployeeCommand.ExecuteAsync(null);

            _mockEmployeeService.Verify(s => s.GetEmployeesAsync(), Times.Never);
        }

        [Fact]
        public async Task AddEmployeeAsync_DialogReturnsTrue_ReloadsEmployees()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());
            Employee? passedEmployee = new Employee { Id = -1 };
            _vm.ShowEntryDialogAsync = emp =>
            {
                passedEmployee = emp;
                return Task.FromResult(true);
            };

            await _vm.AddEmployeeCommand.ExecuteAsync(null);

            Assert.Null(passedEmployee);
            _mockEmployeeService.Verify(s => s.GetEmployeesAsync(), Times.Once);
            Assert.Equal(3, _vm.Employees.Count);
        }

        [Fact]
        public async Task AddEmployeeAsync_DialogReturnsFalse_DoesNotReload()
        {
            _vm.ShowEntryDialogAsync = _ => Task.FromResult(false);

            await _vm.AddEmployeeCommand.ExecuteAsync(null);

            _mockEmployeeService.Verify(s => s.GetEmployeesAsync(), Times.Never);
        }

        [Fact]
        public async Task EditEmployeeAsync_NoTargetSelected_ShowsWarning()
        {
            _vm.SelectedEmployee = null;

            await _vm.EditEmployeeCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select an employee to edit"))), Times.Once);
        }

        [Fact]
        public async Task EditEmployeeAsync_NullParam_UsesSelectedEmployee()
        {
            var selected = new Employee { Id = 9, FirstName = "Selected" };
            _vm.SelectedEmployee = selected;
            Employee? passed = null;
            _vm.ShowEntryDialogAsync = emp =>
            {
                passed = emp;
                return Task.FromResult(false);
            };

            await _vm.EditEmployeeCommand.ExecuteAsync(null);

            Assert.Same(selected, passed);
        }

        [Fact]
        public async Task EditEmployeeAsync_DialogReturnsTrue_Reloads()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());
            var target = new Employee { Id = 1 };
            _vm.ShowEntryDialogAsync = _ => Task.FromResult(true);

            await _vm.EditEmployeeCommand.ExecuteAsync(target);

            _mockEmployeeService.Verify(s => s.GetEmployeesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_NoTargetSelected_ShowsWarning()
        {
            _vm.SelectedEmployee = null;

            await _vm.DeleteEmployeeCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select an employee to delete"))), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_NoConfirmHandler_DoesNotDelete()
        {
            _vm.ConfirmDeleteAsync = null;
            var target = new Employee { Id = 2, FirstName = "Bob" };

            await _vm.DeleteEmployeeCommand.ExecuteAsync(target);

            _mockEmployeeService.Verify(s => s.DeleteEmployeeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_ConfirmedFalse_DoesNotDelete()
        {
            var target = new Employee { Id = 2, FirstName = "Bob", LastName = "Jones" };
            _vm.ConfirmDeleteAsync = _ => Task.FromResult(false);

            await _vm.DeleteEmployeeCommand.ExecuteAsync(target);

            _mockEmployeeService.Verify(s => s.DeleteEmployeeAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_ConfirmedTrue_DeletesAndReloads()
        {
            var target = new Employee { Id = 2, FirstName = "Bob", LastName = "Jones" };
            string? confirmMessage = null;
            _vm.ConfirmDeleteAsync = msg =>
            {
                confirmMessage = msg;
                return Task.FromResult(true);
            };
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());

            await _vm.DeleteEmployeeCommand.ExecuteAsync(target);

            Assert.Equal("Bob Jones", confirmMessage);
            _mockEmployeeService.Verify(s => s.DeleteEmployeeAsync(2), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("Bob"))), Times.Once);
            _mockEmployeeService.Verify(s => s.GetEmployeesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_UsesSelectedEmployeeWhenParamNull()
        {
            var selected = new Employee { Id = 3, FirstName = "Carol", LastName = "Smith" };
            _vm.SelectedEmployee = selected;
            _vm.ConfirmDeleteAsync = _ => Task.FromResult(true);
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(SampleEmployees());

            await _vm.DeleteEmployeeCommand.ExecuteAsync(null);

            _mockEmployeeService.Verify(s => s.DeleteEmployeeAsync(3), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_ServiceThrows_ShowsError()
        {
            var target = new Employee { Id = 2, FirstName = "Bob", LastName = "Jones" };
            _vm.ConfirmDeleteAsync = _ => Task.FromResult(true);
            _mockEmployeeService.Setup(s => s.DeleteEmployeeAsync(2)).ThrowsAsync(new InvalidOperationException("delete fail"));

            await _vm.DeleteEmployeeCommand.ExecuteAsync(target);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("delete fail"))), Times.Once);
        }
    }
}
