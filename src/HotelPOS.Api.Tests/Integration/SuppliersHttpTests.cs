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
    /// Exercises SuppliersController through the real HTTP pipeline (routing, model binding,
    /// JWT auth/RBAC, and the mediator-backed SupplierService) — previously only reachable via
    /// mocked-service controller tests. Mutations require the Purchase permission (not a
    /// "Suppliers" module), since suppliers are managed as part of the purchasing workflow.
    /// </summary>
    public class SuppliersHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SuppliersHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "suppliers.test.user")
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
        public async Task GetSuppliers_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/suppliers");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateSupplier_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "suppliers.cashier-create");

            var response = await client.PostAsJsonAsync("/api/suppliers", new { Name = "Should Not Save" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateSupplier_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "suppliers.admin-create");

            var response = await client.PostAsJsonAsync("/api/suppliers", new
            {
                Name = "Fresh Produce Co",
                Phone = "9876543210",
                Email = "orders@freshproduce.example",
                OpeningBalance = 0m,
                CreditLimit = 10000m
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateSupplier_InvalidEmail_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "suppliers.admin-bad-email");

            var response = await client.PostAsJsonAsync("/api/suppliers", new
            {
                Name = "Bad Email Supplier",
                Email = "not-an-email"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateSupplier_NegativeCreditLimit_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "suppliers.admin-negative-credit");

            var response = await client.PostAsJsonAsync("/api/suppliers", new
            {
                Name = "Negative Credit Supplier",
                CreditLimit = -500m
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetSupplier_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "suppliers.admin-get-missing");

            var response = await client.GetAsync("/api/suppliers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteSupplier_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "suppliers.admin-delete-missing");

            var response = await client.DeleteAsync("/api/suppliers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
