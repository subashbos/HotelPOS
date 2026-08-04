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
    /// Exercises EssController through the real HTTP pipeline. Every action resolves the caller's
    /// own Employee record via ApiUserContext.CurrentUserId -> Employee.UserId, so this suite
    /// doubles as an end-to-end confirmation of the CurrentUserId JWT claim-mapping fix (see
    /// QA_REVIEW_AND_TEST_GAPS.md item 6) — a mocked IUserContext test can't exercise the real
    /// claim resolution this depends on.
    /// </summary>
    public class EssHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EssHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username, int userId)
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username, userId);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        /// <summary>
        /// Employee.UserId is a real required FK to Users, so this seeds an actual User row first
        /// and returns its generated ID for both the Employee link and the caller's JWT "sub" —
        /// an arbitrary int (e.g. 500001) violates the FK the moment the Employee is saved.
        /// </summary>
        private async Task<(int EmployeeId, int UserId)> SeedEmployeeWithLinkedUserAsync(string employeeCode, string username)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var user = new User { Username = username, PasswordHash = "test-hash", Salt = "test-salt", Role = RoleNames.Cashier };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var employee = new Employee { EmployeeCode = employeeCode, FirstName = "Test", DateOfJoining = System.DateTime.UtcNow.Date, UserId = user.Id };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return (employee.Id, user.Id);
        }

        [Fact]
        public async Task GetProfile_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null, username: "n/a", userId: 1);

            var response = await client.GetAsync("/api/ess/profile");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_NoLinkedEmployee_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Cashier, "ess.unlinked", userId: 500001);

            var response = await client.GetAsync("/api/ess/profile");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetProfile_ResolvesOwnEmployeeFromCurrentUserId()
        {
            var (_, userId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-1", "ess.self");
            var client = CreateClient(RoleNames.Cashier, "ess.self", userId);

            var response = await client.GetAsync("/api/ess/profile");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("EMP-ESS-1", dto.GetProperty("employeeCode").GetString());
        }

        [Fact]
        public async Task UpdateProfile_OnlyMutatesAllowedFields()
        {
            var (_, userId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-2", "ess.update-self");
            var client = CreateClient(RoleNames.Cashier, "ess.update-self", userId);

            var response = await client.PutAsJsonAsync("/api/ess/profile", new
            {
                Phone = "9876543210",
                Address = "New Address",
                EmergencyContactName = "Jane Doe",
                EmergencyContactPhone = "9123456789"
            });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await client.GetAsync("/api/ess/profile");
            var dto = await getResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("9876543210", dto.GetProperty("phone").GetString());
            Assert.Equal("New Address", dto.GetProperty("address").GetString());
            // Status/EmploymentType weren't in EssProfileUpdateDto's payload and must be unaffected.
            Assert.Equal(EmployeeStatuses.Active, dto.GetProperty("status").GetString());
        }

        [Fact]
        public async Task GetLeaveBalances_PassesResolvedEmployeeId_NotAnyClientSuppliedValue()
        {
            var (employeeId, userId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-3", "ess.balances-self");
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                context.LeaveTypes.Add(new LeaveType { Code = "CL-ESS3", Name = "Casual Leave", AnnualQuota = 12, IsPaid = true });
                await context.SaveChangesAsync();
            }
            var client = CreateClient(RoleNames.Cashier, "ess.balances-self", userId);

            var response = await client.GetAsync("/api/ess/leave/balances");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var balances = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            foreach (var balance in balances.EnumerateArray())
            {
                Assert.Equal(employeeId, balance.GetProperty("employeeId").GetInt32());
            }
        }

        [Fact]
        public async Task ApplyLeave_IgnoresClientSuppliedEmployeeId_UsesOwnResolvedEmployeeId()
        {
            const int impersonatedEmployeeId = 999999; // does not exist, and must never be used
            var (employeeId, userId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-4", "ess.apply-self");
            int leaveTypeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                var leaveType = new LeaveType { Code = "CL-ESS4", Name = "Casual Leave", AnnualQuota = 12, IsPaid = true };
                context.LeaveTypes.Add(leaveType);
                await context.SaveChangesAsync();
                leaveTypeId = leaveType.Id;
            }
            var client = CreateClient(RoleNames.Cashier, "ess.apply-self", userId);

            var response = await client.PostAsJsonAsync("/api/ess/leave/requests", new
            {
                EmployeeId = impersonatedEmployeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = System.DateTime.UtcNow.Date,
                ToDate = System.DateTime.UtcNow.Date,
                Reason = "Personal"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(employeeId, dto.GetProperty("employeeId").GetInt32());
        }

        [Fact]
        public async Task CancelLeave_NotOwnRequest_ReturnsForbidden()
        {
            var (ownerEmployeeId, ownerUserId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-5", "ess.cancel-owner");
            var (_, otherUserId) = await SeedEmployeeWithLinkedUserAsync("EMP-ESS-6", "ess.cancel-other");
            int leaveTypeId;
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
                var leaveType = new LeaveType { Code = "CL-ESS5", Name = "Casual Leave", AnnualQuota = 12, IsPaid = true };
                context.LeaveTypes.Add(leaveType);
                await context.SaveChangesAsync();
                leaveTypeId = leaveType.Id;
            }

            var ownerClient = CreateClient(RoleNames.Cashier, "ess.cancel-owner", ownerUserId);
            var apply = await ownerClient.PostAsJsonAsync("/api/ess/leave/requests", new
            {
                EmployeeId = ownerEmployeeId,
                LeaveTypeId = leaveTypeId,
                FromDate = System.DateTime.UtcNow.Date,
                ToDate = System.DateTime.UtcNow.Date,
                Reason = "Personal"
            });
            Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
            var applied = await apply.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var requestId = applied.GetProperty("id").GetInt32();

            var otherClient = CreateClient(RoleNames.Cashier, "ess.cancel-other", otherUserId);
            var response = await otherClient.PostAsync($"/api/ess/leave/requests/{requestId}/cancel", content: null);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetPayslips_NoLinkedEmployee_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Cashier, "ess.payslips-unlinked", userId: 500008);

            var response = await client.GetAsync("/api/ess/payslips");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
