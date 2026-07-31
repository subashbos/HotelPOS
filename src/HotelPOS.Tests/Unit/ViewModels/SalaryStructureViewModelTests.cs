using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using HotelPOS.ViewModels;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.ViewModels
{
    public class SalaryStructureViewModelTests
    {
        private readonly Mock<IPayrollService> _payrollServiceMock = new();
        private readonly Mock<IEmployeeService> _employeeServiceMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly SalaryStructureViewModel _viewModel;

        public SalaryStructureViewModelTests()
        {
            _viewModel = new SalaryStructureViewModel(_payrollServiceMock.Object, _employeeServiceMock.Object, _notificationServiceMock.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsEmployeesAndSelectsFirst()
        {
            var emp1 = new Employee { Id = 1, FirstName = "Alice", LastName = "Smith" };
            var emp2 = new Employee { Id = 2, FirstName = "Bob", LastName = "Jones" };

            _employeeServiceMock.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(new List<Employee> { emp1, emp2 });
            _payrollServiceMock.Setup(s => s.GetSalaryStructuresAsync(1)).ReturnsAsync(new List<SalaryStructure>());

            await _viewModel.InitializeAsync();

            Assert.Equal(2, _viewModel.Employees.Count);
            Assert.Equal(emp1, _viewModel.SelectedEmployee);
        }

        [Fact]
        public async Task LoadStructuresAsync_WithStructures_SelectsLatest()
        {
            var emp = new Employee { Id = 10 };
            _viewModel.SelectedEmployee = emp;

            var st1 = new SalaryStructure { Id = 1, EmployeeId = 10, Basic = 50000, EffectiveFrom = new DateTime(2025, 1, 1) };
            var st2 = new SalaryStructure { Id = 2, EmployeeId = 10, Basic = 60000, EffectiveFrom = new DateTime(2026, 1, 1) };

            _payrollServiceMock.Setup(s => s.GetSalaryStructuresAsync(10)).ReturnsAsync(new List<SalaryStructure> { st1, st2 });

            await _viewModel.LoadStructuresAsync();

            Assert.Equal(2, _viewModel.Structures.Count);
            Assert.Equal(st2, _viewModel.SelectedStructure);
            Assert.Equal(60000, _viewModel.Basic);
        }

        [Fact]
        public async Task LoadStructuresAsync_NoStructures_CallsNewStructure()
        {
            var emp = new Employee { Id = 5 };
            _viewModel.SelectedEmployee = emp;

            _payrollServiceMock.Setup(s => s.GetSalaryStructuresAsync(5)).ReturnsAsync(new List<SalaryStructure>());

            await _viewModel.LoadStructuresAsync();

            Assert.Empty(_viewModel.Structures);
            Assert.Null(_viewModel.SelectedStructure);
            Assert.Equal(0, _viewModel.Basic);
        }

        [Fact]
        public void GrossMonthly_RecalculatesWhenAllowancesChange()
        {
            _viewModel.Basic = 40000;
            _viewModel.Hra = 15000;
            _viewModel.Da = 5000;
            _viewModel.ConveyanceAllowance = 2000;
            _viewModel.MedicalAllowance = 1500;
            _viewModel.SpecialAllowance = 6500;

            Assert.Equal(70000, _viewModel.GrossMonthly);
        }

        [Fact]
        public async Task SaveStructureAsync_NoEmployeeSelected_ShowsError()
        {
            _viewModel.SelectedEmployee = null;

            await _viewModel.SaveStructureAsync();

            _notificationServiceMock.Verify(n => n.ShowError("Please select an employee first."), Times.Once);
        }

        [Fact]
        public async Task SaveStructureAsync_ValidEmployee_SavesAndReloads()
        {
            var emp = new Employee { Id = 7, FirstName = "Charlie" };
            _viewModel.SelectedEmployee = emp;

            _viewModel.Basic = 30000;
            _viewModel.Hra = 10000;
            _viewModel.Da = 2000;
            _viewModel.EffectiveFrom = new DateTime(2026, 7, 1);

            _payrollServiceMock.Setup(s => s.GetSalaryStructuresAsync(7)).ReturnsAsync(new List<SalaryStructure>());

            await _viewModel.SaveStructureAsync();

            _payrollServiceMock.Verify(s => s.SaveSalaryStructureAsync(It.Is<SalaryStructure>(st =>
                st.EmployeeId == 7 && st.Basic == 30000 && st.Hra == 10000)), Times.Once);

            _notificationServiceMock.Verify(n => n.ShowSuccess(It.Is<string>(msg => msg.Contains("Charlie"))), Times.Once);
        }

        [Fact]
        public void NewStructure_ResetsAllFields()
        {
            _viewModel.Basic = 50000;
            _viewModel.Hra = 20000;
            _viewModel.EsiApplicable = true;

            _viewModel.NewStructure();

            Assert.Null(_viewModel.SelectedStructure);
            Assert.Equal(0, _viewModel.Basic);
            Assert.Equal(0, _viewModel.Hra);
            Assert.False(_viewModel.EsiApplicable);
            Assert.True(_viewModel.PfApplicable);
        }
    }
}
