using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class DepartmentDesignationViewModelTests
    {
        private readonly Mock<IEmployeeService> _employeeServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly DepartmentDesignationViewModel _viewModel;

        public DepartmentDesignationViewModelTests()
        {
            _viewModel = new DepartmentDesignationViewModel(_employeeServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsDepartmentsAndDesignations()
        {
            var depts = new List<Department> { new() { Id = 1, Name = "HR" } };
            var desigs = new List<Designation> { new() { Id = 10, Title = "Manager", DepartmentId = 1 } };

            _employeeServiceMock.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(depts);
            _employeeServiceMock.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(desigs);

            await _viewModel.InitializeAsync();

            Assert.Single(_viewModel.Departments);
            Assert.Single(_viewModel.Designations);
            Assert.Equal("HR", _viewModel.Departments[0].Name);
            Assert.Equal("Manager", _viewModel.Designations[0].Title);
        }

        [Fact]
        public void NewDepartment_ResetsDepartmentFormFields()
        {
            _viewModel.SelectedDepartment = new Department { Id = 1, Name = "IT" };
            _viewModel.DeptName = "IT";
            _viewModel.DeptDescription = "Tech";

            _viewModel.NewDepartment();

            Assert.Null(_viewModel.SelectedDepartment);
            Assert.Equal(string.Empty, _viewModel.DeptName);
            Assert.Equal(string.Empty, _viewModel.DeptDescription);
        }

        [Fact]
        public async Task SaveDepartmentAsync_Success_SavesAndReloadsData()
        {
            _viewModel.DeptName = "Finance";
            _viewModel.DeptDescription = "Accounting";

            _employeeServiceMock.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new List<Department>());
            _employeeServiceMock.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new List<Designation>());

            await _viewModel.SaveDepartmentAsync();

            _employeeServiceMock.Verify(s => s.SaveDepartmentAsync(It.Is<Department>(d => d.Name == "Finance")), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("Finance"))), Times.Once);
        }

        [Fact]
        public async Task DeleteDepartmentAsync_WhenSelected_DeletesDepartment()
        {
            var dept = new Department { Id = 5, Name = "Logistics" };
            _viewModel.SelectedDepartment = dept;

            _employeeServiceMock.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new List<Department>());
            _employeeServiceMock.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new List<Designation>());

            await _viewModel.DeleteDepartmentAsync();

            _employeeServiceMock.Verify(s => s.DeleteDepartmentAsync(5), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess("Department deleted."), Times.Once);
        }

        [Fact]
        public void OnSelectedDepartmentChanged_PopulatesFields()
        {
            var dept = new Department { Id = 1, Name = "Operations", Description = "Ops Dept" };
            _viewModel.SelectedDepartment = dept;

            Assert.Equal("Operations", _viewModel.DeptName);
            Assert.Equal("Ops Dept", _viewModel.DeptDescription);
        }

        [Fact]
        public async Task SaveDesignationAsync_WithoutDepartment_ShowsError()
        {
            _viewModel.DesigTitle = "Lead";
            _viewModel.DesigDepartment = null;

            await _viewModel.SaveDesignationAsync();

            _notificationServiceMock.Verify(n => n.ShowError("Please select a department for the designation."), Times.Once);
        }

        [Fact]
        public async Task SaveDesignationAsync_WithDepartment_SavesAndReloads()
        {
            var dept = new Department { Id = 2, Name = "Sales" };
            _viewModel.DesigDepartment = dept;
            _viewModel.DesigTitle = "Sales Exec";

            _employeeServiceMock.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new List<Department> { dept });
            _employeeServiceMock.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new List<Designation>());

            await _viewModel.SaveDesignationAsync();

            _employeeServiceMock.Verify(s => s.SaveDesignationAsync(It.Is<Designation>(d => d.Title == "Sales Exec" && d.DepartmentId == 2)), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("Sales Exec"))), Times.Once);
        }

        [Fact]
        public async Task DeleteDesignationAsync_WhenSelected_DeletesDesignation()
        {
            var desig = new Designation { Id = 12, Title = "Intern" };
            _viewModel.SelectedDesignation = desig;

            _employeeServiceMock.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new List<Department>());
            _employeeServiceMock.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new List<Designation>());

            await _viewModel.DeleteDesignationAsync();

            _employeeServiceMock.Verify(s => s.DeleteDesignationAsync(12), Times.Once);
            _notificationServiceMock.Verify(n => n.ShowSuccess("Designation deleted."), Times.Once);
        }

        [Fact]
        public void OnSelectedDesignationChanged_PopulatesFieldsAndMatchingDept()
        {
            var dept = new Department { Id = 3, Name = "Engineering" };
            _viewModel.Departments.Add(dept);

            var desig = new Designation { Id = 101, Title = "Senior Dev", DepartmentId = 3 };
            _viewModel.SelectedDesignation = desig;

            Assert.Equal("Senior Dev", _viewModel.DesigTitle);
            Assert.Equal(dept, _viewModel.DesigDepartment);
        }
    }
}
