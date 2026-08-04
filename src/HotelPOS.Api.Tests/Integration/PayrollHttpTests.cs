using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises PayrollController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed PayrollService) — previously only reachable via
    /// mocked-service controller tests. Runs use distinct month/year combinations per test since
    /// "payroll already run for this month" is real, DB-persisted state shared across the class.
    /// </summary>
    public class PayrollHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PayrollHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "payroll.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [Fact]
        public async Task GetRuns_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/payroll/runs");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetRuns_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "payroll.cashier-runs");

            var response = await client.GetAsync("/api/payroll/runs");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetRuns_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "payroll.admin-runs");

            var response = await client.GetAsync("/api/payroll/runs");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RunPayroll_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "payroll.cashier-run");

            var response = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 1, Year = 2024 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RunPayroll_AdminToken_NoActiveEmployees_CreatesEmptyRun()
        {
            // No employees are seeded, so the run legitimately produces zero payslips — this
            // still exercises the full HTTP + mediator + repository round trip.
            var client = CreateClient(RoleNames.Admin, "payroll.admin-run");

            var response = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 2, Year = 2024 });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RunPayroll_SameMonthTwice_SecondCallReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "payroll.admin-duplicate-run");

            var first = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 3, Year = 2024 });
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 3, Year = 2024 });

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task RunPayroll_InvalidMonth_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "payroll.admin-invalid-month");

            var response = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 13, Year = 2024 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task MarkRunAsPaid_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "payroll.mark-paid-missing");

            var response = await client.PostAsync("/api/payroll/runs/999999/mark-paid", content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task VoidRun_MissingReason_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "payroll.void-run");
            var run = await client.PostAsJsonAsync("/api/payroll/run", new { Month = 4, Year = 2024 });
            Assert.Equal(HttpStatusCode.OK, run.StatusCode);
            var runId = (await run.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();

            var response = await client.PostAsJsonAsync($"/api/payroll/runs/{runId}/void", new { Reason = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
