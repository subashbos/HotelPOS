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
    public class PayrollViewModelTests
    {
        private readonly Mock<IPayrollService> _mockPayrollService = new();
        private readonly Mock<IEmployeeService> _mockEmployeeService = new();
        private readonly Mock<INotificationService> _mockNotif = new();
        private readonly PayrollViewModel _vm;

        public PayrollViewModelTests()
        {
            _mockPayrollService.Setup(s => s.GetRunsAsync()).ReturnsAsync(new List<PayrollRun>());
            _vm = new PayrollViewModel(_mockPayrollService.Object, _mockEmployeeService.Object, _mockNotif.Object);
        }

        [Fact]
        public async Task InitializeAsync_LoadsEmployeesAndRuns()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(new List<Employee>
            {
                new Employee { Id = 1, FirstName = "Alice" }
            });
            _mockPayrollService.Setup(s => s.GetRunsAsync()).ReturnsAsync(new List<PayrollRun>
            {
                new PayrollRun { Id = 1, Month = 6, Year = 2026 }
            });

            await _vm.InitializeAsync();

            Assert.Single(_vm.Employees);
            Assert.Single(_vm.Runs);
        }

        [Fact]
        public async Task InitializeAsync_EmployeeServiceThrows_ShowsError()
        {
            _mockEmployeeService.Setup(s => s.GetEmployeesAsync()).ThrowsAsync(new InvalidOperationException("emp fail"));

            await _vm.InitializeAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load employees") && s.Contains("emp fail"))), Times.Once);
        }

        [Fact]
        public async Task LoadRunsAsync_PopulatesRuns()
        {
            _mockPayrollService.Setup(s => s.GetRunsAsync()).ReturnsAsync(new List<PayrollRun>
            {
                new PayrollRun { Id = 1 },
                new PayrollRun { Id = 2 }
            });

            await _vm.LoadRunsAsync();

            Assert.Equal(2, _vm.Runs.Count);
        }

        [Fact]
        public async Task LoadRunsAsync_Throws_ShowsError()
        {
            _mockPayrollService.Setup(s => s.GetRunsAsync()).ThrowsAsync(new InvalidOperationException("runs fail"));

            await _vm.LoadRunsAsync();

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load payroll runs"))), Times.Once);
        }

        [Fact]
        public void SelectingRun_LoadsPayslips()
        {
            var run = new PayrollRun
            {
                Id = 4,
                Payslips = new List<Payslip> { new Payslip { Id = 1, EmployeeId = 1 } }
            };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(4)).ReturnsAsync(run);

            _vm.SelectedRun = run;

            Assert.Single(_vm.Payslips);
        }

        [Fact]
        public void SelectingRun_NotFound_LeavesPayslipsEmpty()
        {
            var run = new PayrollRun { Id = 5 };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(5)).ReturnsAsync((PayrollRun?)null);

            _vm.SelectedRun = run;

            Assert.Empty(_vm.Payslips);
        }

        [Fact]
        public void SelectingNullRun_ClearsPayslips()
        {
            var run = new PayrollRun { Id = 4, Payslips = new List<Payslip> { new Payslip { Id = 1 } } };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(4)).ReturnsAsync(run);
            _vm.SelectedRun = run;

            _vm.SelectedRun = null;

            Assert.Empty(_vm.Payslips);
        }

        [Fact]
        public void SelectingRun_ServiceThrows_ShowsError()
        {
            var run = new PayrollRun { Id = 6 };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(6)).ThrowsAsync(new InvalidOperationException("payslip fail"));

            _vm.SelectedRun = run;

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("Failed to load payslips"))), Times.Once);
        }

        [Fact]
        public async Task RunPayrollAsync_Success_ShowsSuccessAndReloadsRuns()
        {
            _vm.Month = 7;
            _vm.Year = 2026;
            _mockPayrollService.Setup(s => s.RunPayrollAsync(7, 2026, It.IsAny<int?>()))
                .ReturnsAsync(new PayrollRun { Id = 1 });
            _mockPayrollService.Setup(s => s.GetRunsAsync()).ReturnsAsync(new List<PayrollRun> { new PayrollRun { Id = 1 } });

            await _vm.RunPayrollCommand.ExecuteAsync(null);

            _mockPayrollService.Verify(s => s.RunPayrollAsync(7, 2026, It.IsAny<int?>()), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("processed successfully"))), Times.Once);
            Assert.Single(_vm.Runs);
        }

        [Fact]
        public async Task RunPayrollAsync_ServiceThrows_ShowsError()
        {
            _mockPayrollService.Setup(s => s.RunPayrollAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?>()))
                .ThrowsAsync(new InvalidOperationException("run fail"));

            await _vm.RunPayrollCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("run fail"))), Times.Once);
        }

        [Fact]
        public async Task MarkRunAsPaidAsync_NoRunSelected_ShowsWarning()
        {
            _vm.SelectedRun = null;

            await _vm.MarkRunAsPaidCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select a payroll run"))), Times.Once);
            _mockPayrollService.Verify(s => s.MarkRunAsPaidAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task MarkRunAsPaidAsync_Success_MarksPaidAndReloads()
        {
            var run = new PayrollRun { Id = 9, Month = 8, Year = 2026 };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(9)).ReturnsAsync(run);
            _vm.SelectedRun = run;

            await _vm.MarkRunAsPaidCommand.ExecuteAsync(null);

            _mockPayrollService.Verify(s => s.MarkRunAsPaidAsync(9), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("marked as paid"))), Times.Once);
        }

        [Fact]
        public async Task MarkRunAsPaidAsync_ServiceThrows_ShowsError()
        {
            var run = new PayrollRun { Id = 9 };
            _mockPayrollService.Setup(s => s.GetRunByIdAsync(9)).ReturnsAsync(run);
            _vm.SelectedRun = run;
            _mockPayrollService.Setup(s => s.MarkRunAsPaidAsync(9)).ThrowsAsync(new InvalidOperationException("mark fail"));

            await _vm.MarkRunAsPaidCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("mark fail"))), Times.Once);
        }

        [Fact]
        public async Task SaveSalaryStructureAsync_NoEmployeeSelected_ShowsWarning()
        {
            _vm.SalaryEmployee = null;

            await _vm.SaveSalaryStructureCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowWarning(It.Is<string>(s => s.Contains("select an employee"))), Times.Once);
            _mockPayrollService.Verify(s => s.SaveSalaryStructureAsync(It.IsAny<SalaryStructure>()), Times.Never);
        }

        [Fact]
        public async Task SaveSalaryStructureAsync_Success_SavesAndShowsSuccess()
        {
            _vm.SalaryEmployee = new Employee { Id = 3, FirstName = "Carol" };
            _vm.Basic = 20000;
            _vm.Hra = 8000;
            _vm.Da = 1000;
            _vm.ConveyanceAllowance = 500;
            _vm.MedicalAllowance = 500;
            _vm.SpecialAllowance = 200;

            await _vm.SaveSalaryStructureCommand.ExecuteAsync(null);

            _mockPayrollService.Verify(s => s.SaveSalaryStructureAsync(It.Is<SalaryStructure>(st =>
                st.EmployeeId == 3 && st.Basic == 20000 && st.Hra == 8000)), Times.Once);
            _mockNotif.Verify(n => n.ShowSuccess(It.Is<string>(s => s.Contains("Carol"))), Times.Once);
        }

        [Fact]
        public async Task SaveSalaryStructureAsync_ServiceThrows_ShowsError()
        {
            _vm.SalaryEmployee = new Employee { Id = 3, FirstName = "Carol" };
            _mockPayrollService.Setup(s => s.SaveSalaryStructureAsync(It.IsAny<SalaryStructure>()))
                .ThrowsAsync(new InvalidOperationException("save fail"));

            await _vm.SaveSalaryStructureCommand.ExecuteAsync(null);

            _mockNotif.Verify(n => n.ShowError(It.Is<string>(s => s.Contains("save fail"))), Times.Once);
        }
    }
}
