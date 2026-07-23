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
            _service = new EmployeeService(_repoMock.Object);
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
    }
}
