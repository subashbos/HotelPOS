using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises AttendanceController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the AttendanceService) — previously only reachable via mocked-service
    /// controller tests. Attendance.EmployeeId is a real required FK to Employees, so tests seed a
    /// real Employee first.
    /// </summary>
    public class AttendanceHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AttendanceHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "attendance.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<int> SeedEmployeeAsync(string code)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var employee = new Employee { EmployeeCode = code, FirstName = "Test", DateOfJoining = System.DateTime.UtcNow.Date };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee.Id;
        }

        [Fact]
        public async Task GetAttendance_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/attendance?employeeId=1&from=2026-01-01&to=2026-01-31");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetAttendance_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "attendance.cashier");

            var response = await client.GetAsync("/api/attendance?employeeId=1&from=2026-01-01&to=2026-01-31");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MarkAttendance_AdminToken_ReturnsOkWithComputedWorkedHours()
        {
            var employeeId = await SeedEmployeeAsync("EMP-ATT-1");
            var client = CreateClient(RoleNames.Admin, "attendance.admin-mark");

            var response = await client.PostAsJsonAsync("/api/attendance", new
            {
                EmployeeId = employeeId,
                Date = System.DateTime.UtcNow.Date,
                CheckInTime = "09:00:00",
                CheckOutTime = "17:00:00",
                Status = AttendanceStatuses.Present
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(8m, dto.GetProperty("workedHours").GetDecimal());
        }

        [Fact]
        public async Task MarkAttendance_InvalidStatus_ReturnsBadRequest()
        {
            var employeeId = await SeedEmployeeAsync("EMP-ATT-2");
            var client = CreateClient(RoleNames.Admin, "attendance.admin-bad-status");

            var response = await client.PostAsJsonAsync("/api/attendance", new
            {
                EmployeeId = employeeId,
                Date = System.DateTime.UtcNow.Date,
                Status = "NotARealStatus"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task MarkAttendance_CashierToken_ReturnsForbidden()
        {
            var employeeId = await SeedEmployeeAsync("EMP-ATT-3");
            var client = CreateClient(RoleNames.Cashier, "attendance.cashier-mark");

            var response = await client.PostAsJsonAsync("/api/attendance", new
            {
                EmployeeId = employeeId,
                Date = System.DateTime.UtcNow.Date,
                Status = AttendanceStatuses.Present
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task MarkAttendance_ExistingRecordSameDay_UpdatesInsteadOfDuplicating()
        {
            var employeeId = await SeedEmployeeAsync("EMP-ATT-4");
            var client = CreateClient(RoleNames.Admin, "attendance.admin-remark");
            var date = System.DateTime.UtcNow.Date;

            var first = await client.PostAsJsonAsync("/api/attendance", new { EmployeeId = employeeId, Date = date, Status = AttendanceStatuses.Present });
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var firstDto = await first.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var firstId = firstDto.GetProperty("id").GetInt32();

            var second = await client.PostAsJsonAsync("/api/attendance", new { EmployeeId = employeeId, Date = date, Status = AttendanceStatuses.HalfDay });
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            var secondDto = await second.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

            Assert.Equal(firstId, secondDto.GetProperty("id").GetInt32());
            Assert.Equal(AttendanceStatuses.HalfDay, secondDto.GetProperty("status").GetString());
        }

        [Fact]
        public async Task DeleteAttendance_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "attendance.admin-delete-missing");

            var response = await client.DeleteAsync("/api/attendance/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
