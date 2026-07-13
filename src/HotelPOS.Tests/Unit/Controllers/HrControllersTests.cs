using AutoMapper;
using HotelPOS.Api.Controllers;
using HotelPOS.Application.DTOs.Attendance;
using HotelPOS.Application.DTOs.Employee;
using HotelPOS.Application.DTOs.Leave;
using HotelPOS.Application.DTOs.Payroll;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Unit.Controllers
{
    /// <summary>
    /// Direct-instantiation tests for the HR API controllers (Employees, Leave,
    /// Payroll, Attendance) with mocked services and the real AutoMapper profile,
    /// mirroring the AuthControllerTests approach.
    /// </summary>
    public class HrControllersTests
    {
        private static readonly IMapper Mapper = new MapperConfiguration(
            cfg => cfg.AddProfile(new HotelPOS.Application.Common.Mappings.MappingProfile()),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

        private static Employee Zara() => new()
        {
            Id = 1,
            EmployeeCode = "EMP001",
            FirstName = "Zara",
            DateOfJoining = new DateTime(2024, 1, 1),
            Department = new Department { Id = 1, Name = "Kitchen" }
        };

        // ---------- EmployeesController ----------

        [Fact]
        public async Task Employees_GetEmployees_ReturnsMappedDtos()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.GetEmployeesAsync()).ReturnsAsync(new List<Employee> { Zara() });
            var controller = new EmployeesController(svc.Object, Mapper);

            var result = await controller.GetEmployees();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(ok.Value);
            var dto = Assert.Single(dtos);
            Assert.Equal("Zara", dto.FirstName);
            Assert.Equal("Kitchen", dto.DepartmentName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public async Task Employees_GetEmployee_InvalidId_ReturnsBadRequest(int id)
        {
            var controller = new EmployeesController(Mock.Of<IEmployeeService>(), Mapper);

            var result = await controller.GetEmployee(id);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Employees_GetEmployee_Missing_ReturnsNotFound()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.GetEmployeeByIdAsync(9)).ReturnsAsync((Employee?)null);
            var controller = new EmployeesController(svc.Object, Mapper);

            var result = await controller.GetEmployee(9);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Employees_GetEmployee_Found_ReturnsDto()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.GetEmployeeByIdAsync(1)).ReturnsAsync(Zara());
            var controller = new EmployeesController(svc.Object, Mapper);

            var result = await controller.GetEmployee(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal("EMP001", Assert.IsType<EmployeeDto>(ok.Value).EmployeeCode);
        }

        [Fact]
        public async Task Employees_GetDepartmentsAndDesignations_ReturnMappedDtos()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.GetDepartmentsAsync()).ReturnsAsync(new List<Department> { new() { Id = 1, Name = "Kitchen" } });
            svc.Setup(s => s.GetDesignationsAsync()).ReturnsAsync(new List<Designation>
            {
                new() { Id = 1, Title = "Chef", DepartmentId = 1, Department = new Department { Id = 1, Name = "Kitchen" } }
            });
            var controller = new EmployeesController(svc.Object, Mapper);

            var departments = Assert.IsType<OkObjectResult>((await controller.GetDepartments()).Result);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<DepartmentDto>>(departments.Value));

            var designations = Assert.IsType<OkObjectResult>((await controller.GetDesignations()).Result);
            var designation = Assert.Single(Assert.IsAssignableFrom<IEnumerable<DesignationDto>>(designations.Value));
            Assert.Equal("Kitchen", designation.DepartmentName);
        }

        [Fact]
        public async Task Employees_CreateEmployee_ReturnsCreatedWithNewId()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.SaveEmployeeAsync(It.IsAny<Employee>()))
                .Callback<Employee>(e => e.Id = 77)
                .Returns(Task.CompletedTask);
            var controller = new EmployeesController(svc.Object, Mapper);

            var result = await controller.CreateEmployee(new SaveEmployeeDto { FirstName = "New", EmployeeCode = "EMP009" });

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(77, Assert.IsType<EmployeeDto>(created.Value).Id);
        }

        [Fact]
        public async Task Employees_CreateEmployee_ServiceRejects_ReturnsBadRequest()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.SaveEmployeeAsync(It.IsAny<Employee>()))
                .ThrowsAsync(new ArgumentException("Employee code already exists."));
            var controller = new EmployeesController(svc.Object, Mapper);

            var result = await controller.CreateEmployee(new SaveEmployeeDto { FirstName = "Dup", EmployeeCode = "EMP001" });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Employees_UpdateEmployee_ForcesRouteId()
        {
            Employee? saved = null;
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.SaveEmployeeAsync(It.IsAny<Employee>()))
                .Callback<Employee>(e => saved = e)
                .Returns(Task.CompletedTask);
            var controller = new EmployeesController(svc.Object, Mapper);

            // Body says Id=999; the route id must win.
            var result = await controller.UpdateEmployee(5, new SaveEmployeeDto { Id = 999, FirstName = "Zara" });

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(5, saved!.Id);
        }

        [Fact]
        public async Task Employees_UpdateEmployee_InvalidId_ReturnsBadRequest()
        {
            var controller = new EmployeesController(Mock.Of<IEmployeeService>(), Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.UpdateEmployee(0, new SaveEmployeeDto()));
        }

        [Fact]
        public async Task Employees_DeleteEmployee_MapsOutcomes()
        {
            var svc = new Mock<IEmployeeService>();
            svc.Setup(s => s.DeleteEmployeeAsync(9)).ThrowsAsync(new KeyNotFoundException());
            var controller = new EmployeesController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.DeleteEmployee(0));
            Assert.IsType<NotFoundResult>(await controller.DeleteEmployee(9));
            Assert.IsType<NoContentResult>(await controller.DeleteEmployee(1));
        }

        // ---------- LeaveController ----------

        [Fact]
        public async Task Leave_GetLeaveTypes_ReturnsMappedDtos()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.GetLeaveTypesAsync()).ReturnsAsync(new List<LeaveType>
            {
                new() { Id = 1, Code = "CL", Name = "Casual Leave", AnnualQuota = 12, IsPaid = true }
            });
            var controller = new LeaveController(svc.Object, Mapper);

            var ok = Assert.IsType<OkObjectResult>((await controller.GetLeaveTypes()).Result);
            var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<LeaveTypeDto>>(ok.Value));
            Assert.Equal("Casual Leave", dto.Name);
        }

        [Fact]
        public async Task Leave_GetBalances_InvalidEmployee_ReturnsBadRequest()
        {
            var controller = new LeaveController(Mock.Of<ILeaveService>(), Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.GetBalances(0, null)).Result);
        }

        [Fact]
        public async Task Leave_GetBalances_DefaultsToCurrentYear()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.GetBalancesAsync(1, DateTime.UtcNow.Year)).ReturnsAsync(new List<LeaveBalance>()).Verifiable();
            var controller = new LeaveController(svc.Object, Mapper);

            var result = await controller.GetBalances(1, year: null);

            Assert.IsType<OkObjectResult>(result.Result);
            svc.Verify();
        }

        [Fact]
        public async Task Leave_GetRequests_PassesFiltersThrough()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.GetRequestsAsync(7, "Pending")).ReturnsAsync(new List<LeaveRequest>()).Verifiable();
            var controller = new LeaveController(svc.Object, Mapper);

            Assert.IsType<OkObjectResult>((await controller.GetRequests(7, "Pending")).Result);
            svc.Verify();
        }

        [Fact]
        public async Task Leave_ApplyLeave_ReturnsDto()
        {
            var svc = new Mock<ILeaveService>();
            var controller = new LeaveController(svc.Object, Mapper);

            var result = await controller.ApplyLeave(new ApplyLeaveDto
            {
                EmployeeId = 1,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 8, 3),
                ToDate = new DateTime(2026, 8, 4)
            });

            Assert.IsType<OkObjectResult>(result.Result);
            svc.Verify(s => s.ApplyLeaveAsync(It.Is<LeaveRequest>(r => r.EmployeeId == 1)), Times.Once);
        }

        [Fact]
        public async Task Leave_ApplyLeave_InsufficientBalance_ReturnsBadRequest()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.ApplyLeaveAsync(It.IsAny<LeaveRequest>()))
                .ThrowsAsync(new InvalidOperationException("Insufficient balance."));
            var controller = new LeaveController(svc.Object, Mapper);

            var result = await controller.ApplyLeave(new ApplyLeaveDto { EmployeeId = 1, LeaveTypeId = 1 });

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Leave_ApproveLeave_MapsOutcomes()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.ApproveLeaveAsync(9, 2)).ThrowsAsync(new KeyNotFoundException());
            svc.Setup(s => s.ApproveLeaveAsync(8, 2)).ThrowsAsync(new InvalidOperationException("Already actioned."));
            var controller = new LeaveController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.ApproveLeave(0, 2));
            Assert.IsType<BadRequestObjectResult>(await controller.ApproveLeave(1, 0));
            Assert.IsType<NotFoundResult>(await controller.ApproveLeave(9, 2));
            Assert.IsType<BadRequestObjectResult>(await controller.ApproveLeave(8, 2));
            Assert.IsType<NoContentResult>(await controller.ApproveLeave(1, 2));
        }

        [Fact]
        public async Task Leave_RejectLeave_MapsOutcomes()
        {
            var svc = new Mock<ILeaveService>();
            svc.Setup(s => s.RejectLeaveAsync(9, 2, "reason")).ThrowsAsync(new KeyNotFoundException());
            var controller = new LeaveController(svc.Object, Mapper);
            var dto = new RejectLeaveDto { Reason = "reason" };

            Assert.IsType<BadRequestObjectResult>(await controller.RejectLeave(0, 2, dto));
            Assert.IsType<BadRequestObjectResult>(await controller.RejectLeave(1, 0, dto));
            Assert.IsType<NotFoundResult>(await controller.RejectLeave(9, 2, dto));
            Assert.IsType<NoContentResult>(await controller.RejectLeave(1, 2, dto));
            svc.Verify(s => s.RejectLeaveAsync(1, 2, "reason"), Times.Once);
        }

        // ---------- PayrollController ----------

        [Fact]
        public async Task Payroll_GetSalaryStructures_ValidatesAndMaps()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.GetSalaryStructuresAsync(1)).ReturnsAsync(new List<SalaryStructure>
            {
                new() { Id = 1, EmployeeId = 1, Basic = 20000m, EffectiveFrom = new DateTime(2026, 1, 1) }
            });
            var controller = new PayrollController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.GetSalaryStructures(0)).Result);

            var ok = Assert.IsType<OkObjectResult>((await controller.GetSalaryStructures(1)).Result);
            var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<SalaryStructureDto>>(ok.Value));
            Assert.Equal(20000m, dto.Basic);
        }

        [Fact]
        public async Task Payroll_SaveSalaryStructure_ReturnsDto_AndBadRequestOnValidationError()
        {
            var svc = new Mock<IPayrollService>();
            var controller = new PayrollController(svc.Object, Mapper);

            var ok = await controller.SaveSalaryStructure(new SaveSalaryStructureDto { EmployeeId = 1, Basic = 20000m, EffectiveFrom = new DateTime(2026, 1, 1) });
            Assert.IsType<OkObjectResult>(ok);

            svc.Setup(s => s.SaveSalaryStructureAsync(It.IsAny<SalaryStructure>()))
                .ThrowsAsync(new ArgumentException("Basic must be positive."));
            var bad = await controller.SaveSalaryStructure(new SaveSalaryStructureDto { EmployeeId = 1 });
            Assert.IsType<BadRequestObjectResult>(bad);
        }

        [Fact]
        public async Task Payroll_RunPayroll_ReturnsRunDto()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.RunPayrollAsync(6, 2026, null)).ReturnsAsync(new PayrollRun
            {
                Id = 1,
                Month = 6,
                Year = 2026,
                Payslips = { new Payslip { Id = 1, EmployeeId = 1, NetPay = 24000m, Employee = Zara() } }
            });
            var controller = new PayrollController(svc.Object, Mapper);

            var ok = Assert.IsType<OkObjectResult>((await controller.RunPayroll(new RunPayrollDto { Month = 6, Year = 2026 })).Result);
            var dto = Assert.IsType<PayrollRunDto>(ok.Value);
            Assert.Single(dto.Payslips);
        }

        [Fact]
        public async Task Payroll_RunPayroll_AlreadyProcessed_ReturnsBadRequest()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.RunPayrollAsync(6, 2026, null))
                .ThrowsAsync(new InvalidOperationException("Payroll already processed for this period."));
            var controller = new PayrollController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.RunPayroll(new RunPayrollDto { Month = 6, Year = 2026 })).Result);
        }

        [Fact]
        public async Task Payroll_MarkRunAsPaid_MapsOutcomes()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.MarkRunAsPaidAsync(9)).ThrowsAsync(new KeyNotFoundException());
            svc.Setup(s => s.MarkRunAsPaidAsync(8)).ThrowsAsync(new InvalidOperationException("Run not processed yet."));
            var controller = new PayrollController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.MarkRunAsPaid(0));
            Assert.IsType<NotFoundResult>(await controller.MarkRunAsPaid(9));
            Assert.IsType<BadRequestObjectResult>(await controller.MarkRunAsPaid(8));
            Assert.IsType<NoContentResult>(await controller.MarkRunAsPaid(1));
        }

        [Fact]
        public async Task Payroll_GetRuns_And_GetRun_MapOutcomes()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.GetRunsAsync()).ReturnsAsync(new List<PayrollRun> { new() { Id = 1, Month = 6, Year = 2026 } });
            svc.Setup(s => s.GetRunByIdAsync(1)).ReturnsAsync(new PayrollRun { Id = 1, Month = 6, Year = 2026 });
            svc.Setup(s => s.GetRunByIdAsync(9)).ReturnsAsync((PayrollRun?)null);
            var controller = new PayrollController(svc.Object, Mapper);

            var runs = Assert.IsType<OkObjectResult>((await controller.GetRuns()).Result);
            Assert.Single(Assert.IsAssignableFrom<IEnumerable<PayrollRunDto>>(runs.Value));

            Assert.IsType<BadRequestObjectResult>((await controller.GetRun(0)).Result);
            Assert.IsType<NotFoundResult>((await controller.GetRun(9)).Result);
            Assert.IsType<OkObjectResult>((await controller.GetRun(1)).Result);
        }

        [Fact]
        public async Task Payroll_GetPayslips_ValidatesAndMaps()
        {
            var svc = new Mock<IPayrollService>();
            svc.Setup(s => s.GetPayslipsByEmployeeAsync(1)).ReturnsAsync(new List<Payslip>
            {
                new() { Id = 1, EmployeeId = 1, NetPay = 24000m, Employee = Zara() }
            });
            var controller = new PayrollController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.GetPayslips(0)).Result);

            var ok = Assert.IsType<OkObjectResult>((await controller.GetPayslips(1)).Result);
            var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<PayslipDto>>(ok.Value));
            Assert.Equal(24000m, dto.NetPay);
        }

        // ---------- AttendanceController ----------

        [Fact]
        public async Task Attendance_GetAttendance_ValidatesAndMaps()
        {
            var svc = new Mock<IAttendanceService>();
            svc.Setup(s => s.GetAttendanceAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Attendance> { new() { Id = 1, EmployeeId = 1, Date = new DateTime(2026, 7, 6), WorkedHours = 8, Employee = Zara() } });
            var controller = new AttendanceController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.GetAttendance(0, DateTime.Today, DateTime.Today)).Result);

            var ok = Assert.IsType<OkObjectResult>((await controller.GetAttendance(1, DateTime.Today.AddDays(-7), DateTime.Today)).Result);
            var dto = Assert.Single(Assert.IsAssignableFrom<IEnumerable<AttendanceDto>>(ok.Value));
            Assert.Equal(8, dto.WorkedHours);
        }

        [Fact]
        public async Task Attendance_GetAttendance_BadRange_ReturnsBadRequest()
        {
            var svc = new Mock<IAttendanceService>();
            svc.Setup(s => s.GetAttendanceAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new ArgumentException("From date must be before to date."));
            var controller = new AttendanceController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>((await controller.GetAttendance(1, DateTime.Today, DateTime.Today.AddDays(-1))).Result);
        }

        [Fact]
        public async Task Attendance_MarkAttendance_ReturnsDto()
        {
            var svc = new Mock<IAttendanceService>();
            svc.Setup(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()))
                .ReturnsAsync((Attendance a) => { a.Id = 5; return a; });
            var controller = new AttendanceController(svc.Object, Mapper);

            var result = await controller.MarkAttendance(new MarkAttendanceDto
            {
                EmployeeId = 1,
                Date = new DateTime(2026, 7, 6),
                Status = "Present"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(5, Assert.IsType<AttendanceDto>(ok.Value).Id);
        }

        [Fact]
        public async Task Attendance_MarkAttendance_UnknownEmployee_ReturnsBadRequest()
        {
            var svc = new Mock<IAttendanceService>();
            svc.Setup(s => s.MarkAttendanceAsync(It.IsAny<Attendance>()))
                .ThrowsAsync(new ArgumentException("Employee not found."));
            var controller = new AttendanceController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.MarkAttendance(new MarkAttendanceDto { EmployeeId = 99 }));
        }

        [Fact]
        public async Task Attendance_DeleteAttendance_MapsOutcomes()
        {
            var svc = new Mock<IAttendanceService>();
            svc.Setup(s => s.DeleteAttendanceAsync(9)).ThrowsAsync(new KeyNotFoundException());
            var controller = new AttendanceController(svc.Object, Mapper);

            Assert.IsType<BadRequestObjectResult>(await controller.DeleteAttendance(0));
            Assert.IsType<NotFoundResult>(await controller.DeleteAttendance(9));
            Assert.IsType<NoContentResult>(await controller.DeleteAttendance(1));
        }
    }
}
