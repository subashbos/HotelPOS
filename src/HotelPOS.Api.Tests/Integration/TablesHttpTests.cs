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
    /// Exercises TablesController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed TableService) — previously only reachable via
    /// mocked-service controller tests.
    /// </summary>
    public class TablesHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public TablesHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "tables.test.user")
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
        public async Task GetTables_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/tables");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateTable_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "tables.cashier-create");

            var response = await client.PostAsJsonAsync("/api/tables", new { Number = 101, Name = "T101", Capacity = 4 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateTable_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "tables.admin-create");

            var response = await client.PostAsJsonAsync("/api/tables", new { Number = 201, Name = "T201", Capacity = 4 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateTable_DuplicateNumber_ReturnsConflict()
        {
            var client = CreateClient(RoleNames.Admin, "tables.admin-dupe");
            var first = await client.PostAsJsonAsync("/api/tables", new { Number = 301, Name = "T301", Capacity = 2 });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/tables", new { Number = 301, Name = "T301-Duplicate", Capacity = 2 });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateTable_ZeroNumber_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "tables.admin-zero-number");

            var response = await client.PostAsJsonAsync("/api/tables", new { Number = 0, Name = "Invalid", Capacity = 2 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateTable_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "tables.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/tables/999999", new { Number = 401, Name = "Ghost", Capacity = 2 });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteTable_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "tables.admin-delete-missing");

            var response = await client.DeleteAsync("/api/tables/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
