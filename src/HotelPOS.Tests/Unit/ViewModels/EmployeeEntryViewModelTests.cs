using System;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class EmployeeEntryViewModelTests
    {
        private readonly Mock<IEmployeeService> _mockEmployeeService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly EmployeeEntryViewModel _vm;

        public EmployeeEntryViewModelTests()
        {
            _vm = new EmployeeEntryViewModel(_mockEmployeeService.Object, _mockNotif.Object);
        }

        [Fact]
        public void LoadEmployee_PopulatesPropertiesAndSetsEditMode()
        {
            var employee = new Employee
            {
                Id = 7,
                EmployeeCode = "EMP-007",
                FirstName = "Jane",
                LastName = "Doe",
                Gender = "Female",
                DateOfBirth = new DateTime(1990, 1, 1),
                DateOfJoining = new DateTime(2020, 5, 1),
                DepartmentId = 2,
                DesignationId = 3,
                EmploymentType = EmploymentTypes.Contract,
                Status = EmployeeStatuses.OnLeave,
                Phone = "9876543210",
                Email = "jane@doe.com",
                Address = "123 Street",
                Pan = "ABCDE1234F",
                Aadhaar = "123412341234",
                Uan = "UAN12345",
                EsicNumber = "ESIC1",
                BankName = "Bank",
                BankAccountNumber = "ACC1",
                BankIfsc = "IFSC0001",
                EmergencyContactName = "John Doe",
                EmergencyContactPhone = "1234567890"
            };

            _vm.LoadEmployee(employee);

            Assert.Equal(7, _vm.Id);
            Assert.Equal("EMP-007", _vm.EmployeeCode);
            Assert.Equal("Jane", _vm.FirstName);
            Assert.Equal("Doe", _vm.LastName);
            Assert.Equal("Female", _vm.Gender);
            Assert.Equal(new DateTime(1990, 1, 1), _vm.DateOfBirth);
            Assert.Equal(new DateTime(2020, 5, 1), _vm.DateOfJoining);
            Assert.Equal(2, _vm.DepartmentId);
            Assert.Equal(3, _vm.DesignationId);
            Assert.Equal(EmploymentTypes.Contract, _vm.EmploymentType);
            Assert.Equal(EmployeeStatuses.OnLeave, _vm.Status);
            Assert.Equal("9876543210", _vm.Phone);
            Assert.Equal("jane@doe.com", _vm.Email);
            Assert.Equal("123 Street", _vm.Address);
            Assert.Equal("ABCDE1234F", _vm.Pan);
            Assert.Equal("123412341234", _vm.Aadhaar);
            Assert.Equal("UAN12345", _vm.Uan);
            Assert.Equal("ESIC1", _vm.EsicNumber);
            Assert.Equal("Bank", _vm.BankName);
            Assert.Equal("ACC1", _vm.BankAccountNumber);
            Assert.Equal("IFSC0001", _vm.BankIfsc);
            Assert.Equal("John Doe", _vm.EmergencyContactName);
            Assert.Equal("1234567890", _vm.EmergencyContactPhone);
            Assert.True(_vm.IsEditMode);
            Assert.False(_vm.IsFirstNameInvalid);
            Assert.False(_vm.IsPhoneInvalid);
            Assert.False(_vm.IsEmailInvalid);

            var newEmployee = new Employee { Id = 0, FirstName = "New" };
            _vm.LoadEmployee(newEmployee);
            Assert.False(_vm.IsEditMode);
        }

        [Fact]
        public void LoadEmployee_NullGender_DefaultsToMale()
        {
            var employee = new Employee { Id = 1, FirstName = "Sam", Gender = null };
            _vm.LoadEmployee(employee);
            Assert.Equal("Male", _vm.Gender);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Valid Name", true)]
        public void ValidateFirstName_ChecksForEmpty(string name, bool expectedResult)
        {
            _vm.FirstName = name;
            var result = _vm.ValidateFirstName();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsFirstNameInvalid);
            if (!expectedResult)
            {
                Assert.Equal("First Name is required", _vm.FirstNameError);
            }
            else
            {
                Assert.Empty(_vm.FirstNameError);
            }
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("12345", false)]
        [InlineData("1234567890", true)]
        [InlineData("123456789012345", true)]
        [InlineData("1234567890123456", false)]
        public void ValidatePhone_ChecksLengthRange(string phone, bool expectedResult)
        {
            _vm.Phone = phone;
            var result = _vm.ValidatePhone();
            Assert.Equal(expectedResult, result);
            Assert.Equal(!expectedResult, _vm.IsPhoneInvalid);
        }

        [Theory]
        [InlineData("", true)]
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
            bool? closedResult = null;
            _vm.RequestClose += (sender, result) => closedResult = result;

            _vm.CancelCommand.Execute(null);

            Assert.NotNull(closedResult);
            Assert.False(closedResult.Value);
        }

        [Fact]
        public async Task SaveAsync_InvalidForm_ShowsWarning()
        {
            _vm.FirstName = "";

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.IsAny<string>()), Times.Once);
            _mockEmployeeService.Verify(s => s.SaveEmployeeAsync(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_DuplicateEmployeeCode_ShowsWarningAndSetsError()
        {
            _vm.FirstName = "Valid";
            _vm.EmployeeCode = "DUP-1";
            _mockEmployeeService.Setup(s => s.ValidateEmployeeCodeUniqueAsync("DUP-1", 0)).ReturnsAsync(false);

            await _vm.SaveCommand.ExecuteAsync(null);

            Assert.Contains("already exists", _vm.EmployeeCodeError);
            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("already exists"))), Times.Once);
            _mockEmployeeService.Verify(s => s.SaveEmployeeAsync(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_Success_NewEmployee_SavesAndRequestsClose()
        {
            _vm.FirstName = "New Employee";
            _vm.EmployeeCode = "EMP-100";
            _vm.Phone = "9876543210";
            _vm.Email = "new@employee.com";
            _mockEmployeeService.Setup(s => s.ValidateEmployeeCodeUniqueAsync("EMP-100", 0)).ReturnsAsync(true);

            bool? closeResult = null;
            _vm.RequestClose += (s, success) => closeResult = success;

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockEmployeeService.Verify(s => s.SaveEmployeeAsync(It.Is<Employee>(e =>
                e.FirstName == "New Employee" && e.EmployeeCode == "EMP-100")), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("saved"))), Times.Once);
            Assert.True(closeResult);
        }

        [Fact]
        public async Task SaveAsync_Success_EditMode_ShowsUpdatedMessage()
        {
            _vm.LoadEmployee(new Employee { Id = 55, FirstName = "Existing", EmployeeCode = "EMP-55" });
            _mockEmployeeService.Setup(s => s.ValidateEmployeeCodeUniqueAsync("EMP-55", 55)).ReturnsAsync(true);

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("updated"))), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_EmptyEmployeeCode_SkipsUniquenessCheck()
        {
            _vm.FirstName = "No Code";
            _vm.EmployeeCode = "";

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockEmployeeService.Verify(s => s.ValidateEmployeeCodeUniqueAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _mockEmployeeService.Verify(s => s.SaveEmployeeAsync(It.IsAny<Employee>()), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ServiceThrows_ShowsError()
        {
            _vm.FirstName = "Error Case";
            _vm.EmployeeCode = "EMP-ERR";
            _mockEmployeeService.Setup(s => s.ValidateEmployeeCodeUniqueAsync("EMP-ERR", 0)).ReturnsAsync(true);
            _mockEmployeeService.Setup(s => s.SaveEmployeeAsync(It.IsAny<Employee>())).ThrowsAsync(new InvalidOperationException("db down"));

            await _vm.SaveCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("db down"))), Times.Once);
        }

        [Fact]
        public async Task InitializeAsync_LoadsDepartmentsAndDesignations()
        {
            _mockEmployeeService.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new System.Collections.Generic.List<Department>
            {
                new Department { Id = 1, Name = "Housekeeping" }
            });
            _mockEmployeeService.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new System.Collections.Generic.List<Designation>
            {
                new Designation { Id = 1, Title = "Supervisor" }
            });

            await _vm.InitializeAsync();

            Assert.Single(_vm.Departments);
            Assert.Equal("Housekeeping", _vm.Departments[0].Name);
            Assert.Single(_vm.Designations);
            Assert.Equal("Supervisor", _vm.Designations[0].Title);
        }
    }
}
