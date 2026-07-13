using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises the HR persistence layer (employees, attendance, leave, payroll)
    /// against the EF InMemory provider, mirroring RepositoryIntegrationTests.
    /// </summary>
    public class HrRepositoryTests
    {
        private static HotelDbContext GetContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new HotelDbContext(options);
        }

        private static Employee NewEmployee(int id, string code, string firstName) => new()
        {
            Id = id,
            EmployeeCode = code,
            FirstName = firstName,
            DateOfJoining = new DateTime(2024, 1, 1)
        };

        [Fact]
        public async Task EmployeeRepository_CrudAndLookups()
        {
            using var context = GetContext(nameof(EmployeeRepository_CrudAndLookups));
            var repo = new EmployeeRepository(context);

            context.Departments.Add(new Department { Id = 1, Name = "Kitchen" });
            context.Designations.Add(new Designation { Id = 1, Title = "Chef", DepartmentId = 1 });
            await context.SaveChangesAsync();

            await repo.AddAsync(NewEmployee(1, "EMP001", "Zara"));
            var second = NewEmployee(2, "EMP002", "Anil");
            second.DepartmentId = 1;
            second.DesignationId = 1;
            second.ReportingManagerId = 1;
            await repo.AddAsync(second);

            // GetAll orders by first name and includes navigations
            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count);
            Assert.Equal("Anil", all[0].FirstName);
            Assert.Equal("Kitchen", all[0].Department!.Name);
            Assert.Equal("Chef", all[0].Designation!.Title);
            Assert.Equal("Zara", all[0].ReportingManager!.FirstName);

            var byId = await repo.GetByIdAsync(2);
            Assert.NotNull(byId);
            Assert.Equal("EMP002", byId!.EmployeeCode);

            // Code lookups are case-insensitive
            Assert.NotNull(await repo.GetByCodeAsync("emp001"));
            Assert.True(await repo.ExistsByCodeAsync("EMP001"));
            Assert.False(await repo.ExistsByCodeAsync("EMP001", excludeId: 1));
            Assert.False(await repo.ExistsByCodeAsync("EMP999"));

            byId.FirstName = "Anil Kumar";
            await repo.UpdateAsync(byId);
            Assert.Equal("Anil Kumar", (await repo.GetByIdAsync(2))!.FirstName);

            await repo.DeleteAsync(2);
            Assert.Null(await repo.GetByIdAsync(2));
            await repo.DeleteAsync(999); // deleting a missing employee is a no-op

            Assert.Single(await repo.GetDepartmentsAsync());
            var designations = await repo.GetDesignationsAsync();
            Assert.Single(designations);
            Assert.Equal("Kitchen", designations[0].Department!.Name);
        }

        [Fact]
        public async Task AttendanceRepository_CrudAndRangeQueries()
        {
            using var context = GetContext(nameof(AttendanceRepository_CrudAndRangeQueries));
            var repo = new AttendanceRepository(context);

            context.Employees.Add(NewEmployee(1, "EMP001", "Zara"));
            await context.SaveChangesAsync();

            var monday = new DateTime(2026, 7, 6);
            await repo.AddAsync(new Attendance { Id = 1, EmployeeId = 1, Date = monday, WorkedHours = 8 });
            await repo.AddAsync(new Attendance { Id = 2, EmployeeId = 1, Date = monday.AddDays(1), WorkedHours = 4, Status = AttendanceStatuses.HalfDay });
            await repo.AddAsync(new Attendance { Id = 3, EmployeeId = 1, Date = monday.AddDays(10), WorkedHours = 8 });

            var week = await repo.GetByEmployeeAsync(1, monday, monday.AddDays(6));
            Assert.Equal(2, week.Count);
            Assert.Equal(monday, week[0].Date); // ordered by date

            var range = await repo.GetByDateRangeAsync(monday, monday.AddDays(6));
            Assert.Equal(2, range.Count);
            Assert.Equal("Zara", range[0].Employee!.FirstName);

            var single = await repo.GetByEmployeeAndDateAsync(1, monday.AddHours(15)); // time component ignored
            Assert.NotNull(single);
            Assert.Equal(1, single!.Id);
            Assert.Null(await repo.GetByEmployeeAndDateAsync(1, monday.AddDays(2)));

            var byId = await repo.GetByIdAsync(2);
            Assert.NotNull(byId);
            Assert.Equal("Zara", byId!.Employee!.FirstName);

            byId.WorkedHours = 8;
            byId.Status = AttendanceStatuses.Present;
            await repo.UpdateAsync(byId);
            Assert.Equal(8, (await repo.GetByIdAsync(2))!.WorkedHours);

            await repo.DeleteAsync(3);
            Assert.Null(await repo.GetByIdAsync(3));
            await repo.DeleteAsync(999); // no-op
        }

        [Fact]
        public async Task LeaveRepository_TypesBalancesAndRequests()
        {
            using var context = GetContext(nameof(LeaveRepository_TypesBalancesAndRequests));
            var repo = new LeaveRepository(context);

            context.Employees.Add(NewEmployee(1, "EMP001", "Zara"));
            context.LeaveTypes.AddRange(
                new LeaveType { Id = 1, Code = LeaveTypeCodes.CasualLeave, Name = "Casual Leave", AnnualQuota = 12, IsPaid = true },
                new LeaveType { Id = 2, Code = LeaveTypeCodes.SickLeave, Name = "Sick Leave", AnnualQuota = 6, IsPaid = true });
            await context.SaveChangesAsync();

            var types = await repo.GetLeaveTypesAsync();
            Assert.Equal(2, types.Count);
            Assert.Equal("Casual Leave", types[0].Name); // ordered by name
            Assert.NotNull(await repo.GetLeaveTypeByIdAsync(2));

            await repo.AddBalanceAsync(new LeaveBalance { Id = 1, EmployeeId = 1, LeaveTypeId = 1, Year = 2026, EntitledDays = 12, UsedDays = 2 });

            var balance = await repo.GetBalanceAsync(1, 1, 2026);
            Assert.NotNull(balance);
            Assert.Equal("Casual Leave", balance!.LeaveType!.Name);
            Assert.Null(await repo.GetBalanceAsync(1, 1, 2025));

            balance.UsedDays = 3;
            await repo.UpdateBalanceAsync(balance);
            var balances = await repo.GetBalancesAsync(1, 2026);
            Assert.Single(balances);
            Assert.Equal(3, balances[0].UsedDays);

            await repo.AddRequestAsync(new LeaveRequest
            {
                Id = 1,
                EmployeeId = 1,
                LeaveTypeId = 1,
                FromDate = new DateTime(2026, 8, 3),
                ToDate = new DateTime(2026, 8, 4),
                TotalDays = 2,
                Status = LeaveRequestStatuses.Pending,
                AppliedOn = DateTime.UtcNow.AddDays(-1)
            });
            await repo.AddRequestAsync(new LeaveRequest
            {
                Id = 2,
                EmployeeId = 1,
                LeaveTypeId = 2,
                FromDate = new DateTime(2026, 8, 10),
                ToDate = new DateTime(2026, 8, 10),
                TotalDays = 1,
                Status = LeaveRequestStatuses.Approved,
                AppliedOn = DateTime.UtcNow
            });

            var allRequests = await repo.GetRequestsAsync();
            Assert.Equal(2, allRequests.Count);
            Assert.Equal(2, allRequests[0].Id); // newest applied first

            Assert.Equal(2, (await repo.GetRequestsAsync(employeeId: 1)).Count);
            Assert.Empty(await repo.GetRequestsAsync(employeeId: 99));
            var pending = await repo.GetRequestsAsync(status: LeaveRequestStatuses.Pending);
            Assert.Single(pending);
            Assert.Equal(1, pending[0].Id);

            var request = await repo.GetRequestByIdAsync(1);
            Assert.NotNull(request);
            Assert.Equal("Zara", request!.Employee!.FirstName);
            Assert.Equal("Casual Leave", request.LeaveType!.Name);

            request.Status = LeaveRequestStatuses.Rejected;
            request.RejectionReason = "Short staffed";
            await repo.UpdateRequestAsync(request);
            Assert.Equal(LeaveRequestStatuses.Rejected, (await repo.GetRequestByIdAsync(1))!.Status);
        }

        [Fact]
        public async Task PayrollRepository_SalaryStructuresAndRuns()
        {
            using var context = GetContext(nameof(PayrollRepository_SalaryStructuresAndRuns));
            var repo = new PayrollRepository(context);

            context.Employees.Add(NewEmployee(1, "EMP001", "Zara"));
            await context.SaveChangesAsync();

            await repo.AddSalaryStructureAsync(new SalaryStructure
            {
                Id = 1,
                EmployeeId = 1,
                EffectiveFrom = new DateTime(2025, 1, 1),
                EffectiveTo = new DateTime(2025, 12, 31),
                Basic = 20000m
            });
            await repo.AddSalaryStructureAsync(new SalaryStructure
            {
                Id = 2,
                EmployeeId = 1,
                EffectiveFrom = new DateTime(2026, 1, 1),
                Basic = 25000m
            });

            // Picks the structure effective on the given date; open-ended EffectiveTo matches.
            var current = await repo.GetCurrentSalaryStructureAsync(1, new DateTime(2026, 7, 1));
            Assert.NotNull(current);
            Assert.Equal(25000m, current!.Basic);

            var old = await repo.GetCurrentSalaryStructureAsync(1, new DateTime(2025, 6, 1));
            Assert.Equal(20000m, old!.Basic);

            Assert.Null(await repo.GetCurrentSalaryStructureAsync(1, new DateTime(2024, 1, 1)));
            Assert.Null(await repo.GetCurrentSalaryStructureAsync(99, new DateTime(2026, 1, 1)));

            var structures = await repo.GetSalaryStructuresAsync(1);
            Assert.Equal(2, structures.Count);
            Assert.Equal(25000m, structures[0].Basic); // newest EffectiveFrom first

            current.Basic = 26000m;
            await repo.UpdateSalaryStructureAsync(current);
            Assert.Equal(26000m, (await repo.GetCurrentSalaryStructureAsync(1, new DateTime(2026, 7, 1)))!.Basic);

            await repo.AddRunAsync(new PayrollRun
            {
                Id = 1,
                Month = 6,
                Year = 2026,
                Payslips = { new Payslip { Id = 1, EmployeeId = 1, NetPay = 24000m } }
            });
            await repo.AddRunAsync(new PayrollRun { Id = 2, Month = 5, Year = 2026 });

            var run = await repo.GetRunAsync(6, 2026);
            Assert.NotNull(run);
            Assert.Single(run!.Payslips);
            Assert.Null(await repo.GetRunAsync(12, 2020));

            var runById = await repo.GetRunByIdAsync(1);
            Assert.NotNull(runById);
            Assert.Equal("Zara", runById!.Payslips[0].Employee!.FirstName);

            var runs = await repo.GetRunsAsync();
            Assert.Equal(2, runs.Count);
            Assert.Equal(6, runs[0].Month); // newest year+month first

            run.Status = PayrollRunStatuses.Processed;
            run.ProcessedOn = DateTime.UtcNow;
            await repo.UpdateRunAsync(run);
            Assert.Equal(PayrollRunStatuses.Processed, (await repo.GetRunAsync(6, 2026))!.Status);

            var payslips = await repo.GetPayslipsByEmployeeAsync(1);
            Assert.Single(payslips);
            Assert.Equal(24000m, payslips[0].NetPay);
            Assert.NotNull(payslips[0].PayrollRun);
            Assert.Empty(await repo.GetPayslipsByEmployeeAsync(99));
        }
    }
}
