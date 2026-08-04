using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises CustomersController through the real HTTP pipeline (routing, model binding,
    /// JWT auth/RBAC, and the mediator-backed CustomerService) — previously only reachable via
    /// mocked-service controller tests.
    ///
    /// Customers has a permission split worth locking in precisely: the seeded Cashier role is
    /// actually granted the "Customers" module (create/update), but NOT "CustomerManagement"
    /// (delete) — the opposite of most modules, where Cashier gets nothing. Critically, the
    /// unauthenticated-role *fallback* AuthorizationService falls back to when a caller has no
    /// real seeded User row only grants Cashier Billing/Shift, which would make a bare Cashier
    /// JWT incorrectly appear forbidden from creating customers. So the "Cashier can create"
    /// tests here seed a real User row with RoleId=2 to force the real DB-driven permission
    /// lookup, rather than relying on the fallback like the other controllers' tests do.
    /// </summary>
    public class CustomersHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int CashierRoleId = 2;
        private readonly CustomWebApplicationFactory _factory;

        public CustomersHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "customers.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task SeedCashierUserAsync(string username)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            context.Users.Add(new User
            {
                Username = username,
                PasswordHash = "test-hash",
                Salt = "test-salt",
                Role = RoleNames.Cashier,
                RoleId = CashierRoleId
            });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetCustomers_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/customers");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCustomers_CashierFallback_ReturnsOk()
        {
            // GET has no permission check at all, so even a bare (unseeded) Cashier JWT works.
            var client = CreateClient(RoleNames.Cashier, "customers.cashier-list");

            var response = await client.GetAsync("/api/customers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateCustomer_RealCashierAccount_ReturnsCreated()
        {
            const string username = "customers.real-cashier-create";
            await SeedCashierUserAsync(username);
            var client = CreateClient(RoleNames.Cashier, username);

            var response = await client.PostAsJsonAsync("/api/customers", new { Name = "Walk-in Regular", Phone = "9876543210" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCustomer_RealCashierAccount_ReturnsForbidden()
        {
            // Same seeded Cashier account that CAN create customers must still be blocked from
            // deleting one — Customers and CustomerManagement are gated separately.
            const string creatorUsername = "customers.delete-setup-admin";
            var adminClient = CreateClient(RoleNames.Admin, creatorUsername);
            var create = await adminClient.PostAsJsonAsync("/api/customers", new { Name = "To Be Protected", Phone = "9876500000" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var customerId = created.GetProperty("id").GetInt32();

            const string cashierUsername = "customers.real-cashier-delete";
            await SeedCashierUserAsync(cashierUsername);
            var cashierClient = CreateClient(RoleNames.Cashier, cashierUsername);

            var response = await cashierClient.DeleteAsync($"/api/customers/{customerId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCustomer_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "customers.admin-create");

            var response = await client.PostAsJsonAsync("/api/customers", new { Name = "Admin Created Customer", Phone = "9123456789" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateCustomer_DuplicatePhone_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "customers.admin-dupe-phone");
            var first = await client.PostAsJsonAsync("/api/customers", new { Name = "First Customer", Phone = "9000000001" });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/customers", new { Name = "Second Customer", Phone = "9000000001" });

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task GetCustomer_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "customers.admin-get-missing");

            var response = await client.GetAsync("/api/customers/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCustomer_AdminToken_ReturnsNoContent()
        {
            var adminClient = CreateClient(RoleNames.Admin, "customers.admin-delete");
            var create = await adminClient.PostAsJsonAsync("/api/customers", new { Name = "Deletable Customer", Phone = "9111111111" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var customerId = created.GetProperty("id").GetInt32();

            var response = await adminClient.DeleteAsync($"/api/customers/{customerId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
