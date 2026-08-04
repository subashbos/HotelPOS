using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises AuditController through the real HTTP pipeline (routing, JWT auth/RBAC, and the
    /// mediator-backed AuditService) — previously only reachable via mocked-service controller
    /// tests.
    /// </summary>
    public class AuditHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public AuditHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "audit.test.user")
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
        public async Task GetLogs_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/audit");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetLogs_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "audit.cashier");

            var response = await client.GetAsync("/api/audit");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetLogs_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "audit.admin");

            var response = await client.GetAsync("/api/audit");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetLogs_WithDateRange_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "audit.admin-range");

            var response = await client.GetAsync("/api/audit?from=2026-01-01&to=2026-12-31");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
