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
    /// Exercises LeaveController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the LeaveService) — previously only reachable via mocked-service controller
    /// tests. No LeaveType is seeded by default and there's no API to create one, so tests seed a
    /// real LeaveType directly via the DB.
    /// </summary>
    public class LeaveHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public LeaveHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "leave.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<(int EmployeeId, int LeaveTypeId)> SeedEmployeeAndLeaveTypeAsync(string employeeCode, string leaveTypeCode)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var employee = new Employee { EmployeeCode = employeeCode, FirstName = "Test", DateOfJoining = System.DateTime.UtcNow.Date };
            var leaveType = new LeaveType { Code = leaveTypeCode, Name = "Casual Leave", AnnualQuota = 12, IsPaid = true };
            context.Employees.Add(employee);
            context.LeaveTypes.Add(leaveType);
            await context.SaveChangesAsync();
            return (employee.Id, leaveType.Id);
        }

        [Fact]
        public async Task GetLeaveTypes_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/leave/types");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLeaveTypes_CashierToken_ReturnsOk()
        {
            // GetLeaveTypesAsync has no permission check — any authenticated caller can list them.
            var client = CreateClient(RoleNames.Cashier, "leave.cashier-types");

            var response = await client.GetAsync("/api/leave/types");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetBalances_CashierToken_ReturnsForbidden()
        {
            var (employeeId, _) = await SeedEmployeeAndLeaveTypeAsync("EMP-LV-1", "CL1");
            var client = CreateClient(RoleNames.Cashier, "leave.cashier-balances");

            var response = await client.GetAsync($"/api/leave/balances/{employeeId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ApplyLeave_AdminToken_SufficientBalance_ReturnsOk()
        {
            var (employeeId, leaveTypeId) = await SeedEmployeeAndLeaveTypeAsync("EMP-LV-2", "CL2");
            var client = CreateClient(RoleNames.Admin, "leave.admin-apply");

            var response = await client.PostAsJsonAsync("/api/leave/requests", new
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = System.DateTime.UtcNow.Date,
                ToDate = System.DateTime.UtcNow.Date,
                Reason = "Personal"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ApplyLeave_InsufficientBalance_ReturnsBadRequest()
        {
            var (employeeId, leaveTypeId) = await SeedEmployeeAndLeaveTypeAsync("EMP-LV-3", "CL3");
            var client = CreateClient(RoleNames.Admin, "leave.admin-insufficient");

            var response = await client.PostAsJsonAsync("/api/leave/requests", new
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = System.DateTime.UtcNow.Date,
                // 12 day quota, requesting 20 - should exceed the available balance.
                ToDate = System.DateTime.UtcNow.Date.AddDays(19),
                Reason = "Too long"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApplyLeave_UnknownLeaveType_ReturnsBadRequest()
        {
            int employeeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                var employee = new Employee { EmployeeCode = "EMP-LV-4", FirstName = "Test", DateOfJoining = System.DateTime.UtcNow.Date };
                context.Employees.Add(employee);
                await context.SaveChangesAsync();
                employeeId = employee.Id;
            }
            var client = CreateClient(RoleNames.Admin, "leave.admin-unknown-type");

            var response = await client.PostAsJsonAsync("/api/leave/requests", new
            {
                EmployeeId = employeeId,
                LeaveTypeId = 999999,
                FromDate = System.DateTime.UtcNow.Date,
                ToDate = System.DateTime.UtcNow.Date,
                Reason = "Bad leave type reference"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ApproveLeave_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "leave.cashier-approve");

            var response = await client.PostAsync("/api/leave/requests/1/approve?approverEmployeeId=1", content: null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ApproveLeave_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "leave.admin-approve-missing");

            var response = await client.PostAsync("/api/leave/requests/999999/approve?approverEmployeeId=1", content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApproveLeave_AdminToken_PendingRequest_ApprovesAndDeductsBalance()
        {
            var (employeeId, leaveTypeId) = await SeedEmployeeAndLeaveTypeAsync("EMP-LV-5", "CL5");
            var client = CreateClient(RoleNames.Admin, "leave.admin-approve");

            var apply = await client.PostAsJsonAsync("/api/leave/requests", new
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = System.DateTime.UtcNow.Date,
                ToDate = System.DateTime.UtcNow.Date,
                Reason = "Personal"
            });
            Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
            var applied = await apply.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var requestId = applied.GetProperty("id").GetInt32();

            var response = await client.PostAsync($"/api/leave/requests/{requestId}/approve?approverEmployeeId={employeeId}", content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RejectLeave_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "leave.admin-reject-missing");

            var response = await client.PostAsJsonAsync("/api/leave/requests/999999/reject?approverEmployeeId=1", new { Reason = "No longer needed" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
