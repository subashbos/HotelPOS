using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Unit.Services
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IEmployeeRepository> _repoMock;
        private readonly EmployeeService _service;

        public EmployeeServiceTests()
        {
            _repoMock = new Mock<IEmployeeRepository>();
            _service = new EmployeeService(_repoMock.Object, TestAuthorization.AllowAll().Object);
        }

        [Fact]
        public async Task SaveEmployeeAsync_ValidNewEmployee_ShouldSaveSuccessfully()
        {
            var employee = new Employee
            {
                Id = 0,
                EmployeeCode = "EMP0010",
                FirstName = "Asha",
                DateOfJoining = DateTime.Today
            };

            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0010", 0)).ReturnsAsync(false);

            await _service.SaveEmployeeAsync(employee);

            _repoMock.Verify(r => r.AddAsync(employee), Times.Once);
        }

        [Fact]
        public async Task SaveEmployeeAsync_EmptyFirstName_ShouldThrowArgumentException()
        {
            var employee = new Employee
            {
                EmployeeCode = "EMP0011",
                FirstName = "",
                DateOfJoining = DateTime.Today
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveEmployeeAsync(employee));
            Assert.Contains("First Name is required", ex.Message);
        }

        [Fact]
        public async Task SaveEmployeeAsync_BlankCode_GeneratesNextSequentialCode()
        {
            var employee = new Employee
            {
                EmployeeCode = "",
                FirstName = "Ravi",
                DateOfJoining = DateTime.Today
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Employee>
            {
                new Employee { EmployeeCode = "EMP0001" },
                new Employee { EmployeeCode = "EMP0003" }
            });
            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0004", 0)).ReturnsAsync(false);

            await _service.SaveEmployeeAsync(employee);

            Assert.Equal("EMP0004", employee.EmployeeCode);
            _repoMock.Verify(r => r.AddAsync(employee), Times.Once);
        }

        [Fact]
        public async Task SaveEmployeeAsync_DuplicateCode_ShouldThrowArgumentException()
        {
            var employee = new Employee
            {
                Id = 0,
                EmployeeCode = "EMP0001",
                FirstName = "Meera",
                DateOfJoining = DateTime.Today
            };

            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0001", 0)).ReturnsAsync(true);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveEmployeeAsync(employee));
            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task DeleteEmployeeAsync_NotFound_ThrowsKeyNotFoundException()
        {
            _repoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Employee?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteEmployeeAsync(999));
        }

        [Fact]
        public async Task SaveEmployeeAsync_ExistingEmployee_ShouldUpdate()
        {
            var employee = new Employee
            {
                Id = 3,
                EmployeeCode = "EMP0003",
                FirstName = "Kiran",
                DateOfJoining = DateTime.Today
            };

            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0003", 3)).ReturnsAsync(false);

            await _service.SaveEmployeeAsync(employee);

            _repoMock.Verify(r => r.UpdateAsync(employee), Times.Once);
            _repoMock.Verify(r => r.AddAsync(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task GetEmployeesAsync_ReturnsAllFromRepository()
        {
            var employees = new List<Employee> { new Employee { Id = 1, EmployeeCode = "EMP0001" } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(employees);

            var result = await _service.GetEmployeesAsync();

            Assert.Same(employees, result);
        }

        [Fact]
        public async Task GetEmployeesAsync_NullFromRepo_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync((List<Employee>)null!);

            var result = await _service.GetEmployeesAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetEmployeeByIdAsync_ReturnsEmployeeFromRepository()
        {
            var employee = new Employee { Id = 5, EmployeeCode = "EMP0005" };
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(employee);

            var result = await _service.GetEmployeeByIdAsync(5);

            Assert.Same(employee, result);
        }

        [Fact]
        public async Task ValidateEmployeeCodeUniqueAsync_BlankCode_ReturnsFalse()
        {
            var result = await _service.ValidateEmployeeCodeUniqueAsync("   ");

            Assert.False(result);
            _repoMock.Verify(r => r.ExistsByCodeAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ValidateEmployeeCodeUniqueAsync_UniqueCode_ReturnsTrue()
        {
            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0099", 0)).ReturnsAsync(false);

            var result = await _service.ValidateEmployeeCodeUniqueAsync("EMP0099");

            Assert.True(result);
        }

        [Fact]
        public async Task ValidateEmployeeCodeUniqueAsync_DuplicateCode_ReturnsFalse()
        {
            _repoMock.Setup(r => r.ExistsByCodeAsync("EMP0001", 0)).ReturnsAsync(true);

            var result = await _service.ValidateEmployeeCodeUniqueAsync("EMP0001");

            Assert.False(result);
        }

        [Fact]
        public async Task GetDepartmentsAsync_ReturnsFromRepository()
        {
            var departments = new List<Department> { new Department { Id = 1, Name = "Kitchen" } };
            _repoMock.Setup(r => r.GetDepartmentsAsync()).ReturnsAsync(departments);

            var result = await _service.GetDepartmentsAsync();

            Assert.Same(departments, result);
        }

        [Fact]
        public async Task SaveDepartmentAsync_NullDepartment_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveDepartmentAsync(null!));
        }

        [Fact]
        public async Task SaveDepartmentAsync_EmptyName_ThrowsArgumentException()
        {
            var department = new Department { Name = "" };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveDepartmentAsync(department));
            Assert.Contains("Department name is required", ex.Message);
        }

        [Fact]
        public async Task SaveDepartmentAsync_Valid_CallsRepository()
        {
            var department = new Department { Name = "Housekeeping" };

            await _service.SaveDepartmentAsync(department);

            _repoMock.Verify(r => r.SaveDepartmentAsync(department), Times.Once);
        }

        [Fact]
        public async Task DeleteDepartmentAsync_CallsRepository()
        {
            await _service.DeleteDepartmentAsync(4);

            _repoMock.Verify(r => r.DeleteDepartmentAsync(4), Times.Once);
        }

        [Fact]
        public async Task GetDesignationsAsync_ReturnsFromRepository()
        {
            var designations = new List<Designation> { new Designation { Id = 1, Title = "Chef" } };
            _repoMock.Setup(r => r.GetDesignationsAsync()).ReturnsAsync(designations);

            var result = await _service.GetDesignationsAsync();

            Assert.Same(designations, result);
        }

        [Fact]
        public async Task SaveDesignationAsync_NullDesignation_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveDesignationAsync(null!));
        }

        [Fact]
        public async Task SaveDesignationAsync_EmptyTitle_ThrowsArgumentException()
        {
            var designation = new Designation { Title = "", DepartmentId = 1 };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveDesignationAsync(designation));
            Assert.Contains("Designation title is required", ex.Message);
        }

        [Fact]
        public async Task SaveDesignationAsync_InvalidDepartmentId_ThrowsArgumentException()
        {
            var designation = new Designation { Title = "Chef", DepartmentId = 0 };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveDesignationAsync(designation));
            Assert.Contains("Please select a valid department", ex.Message);
        }

        [Fact]
        public async Task SaveDesignationAsync_Valid_CallsRepository()
        {
            var designation = new Designation { Title = "Chef", DepartmentId = 2 };

            await _service.SaveDesignationAsync(designation);

            _repoMock.Verify(r => r.SaveDesignationAsync(designation), Times.Once);
        }

        [Fact]
        public async Task DeleteDesignationAsync_CallsRepository()
        {
            await _service.DeleteDesignationAsync(7);

            _repoMock.Verify(r => r.DeleteDesignationAsync(7), Times.Once);
        }
    }
}
