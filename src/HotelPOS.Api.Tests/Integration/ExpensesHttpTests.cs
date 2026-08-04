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
    /// Exercises ExpensesController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed ExpenseService) — previously only reachable via
    /// mocked-service controller tests.
    /// </summary>
    public class ExpensesHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ExpensesHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "expenses.test.user", int userId = 1)
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username, userId);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        [Fact]
        public async Task GetExpenses_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/expenses");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateExpense_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "expenses.cashier-create");

            var response = await client.PostAsJsonAsync("/api/expenses", new
            {
                Date = System.DateTime.UtcNow,
                Title = "Should Not Save",
                Amount = 100m,
                Category = ExpenseCategories.General
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateExpense_AdminToken_ReturnsCreatedWithCreatedByFromToken()
        {
            var client = CreateClient(RoleNames.Admin, "expenses.admin-create", userId: 42);

            var response = await client.PostAsJsonAsync("/api/expenses", new
            {
                Date = System.DateTime.UtcNow,
                Title = "Office Supplies",
                Amount = 250.50m,
                Category = ExpenseCategories.General
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(42, dto.GetProperty("createdBy").GetInt32());
        }

        [Fact]
        public async Task CreateExpense_NonPositiveAmount_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "expenses.admin-bad-amount");

            var response = await client.PostAsJsonAsync("/api/expenses", new
            {
                Date = System.DateTime.UtcNow,
                Title = "Zero Amount",
                Amount = 0m,
                Category = ExpenseCategories.General
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateExpense_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "expenses.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/expenses/999999", new
            {
                Date = System.DateTime.UtcNow,
                Title = "Ghost Expense",
                Amount = 10m,
                Category = ExpenseCategories.General
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteExpense_CashierToken_ReturnsForbidden()
        {
            var adminClient = CreateClient(RoleNames.Admin, "expenses.admin-delete-setup");
            var create = await adminClient.PostAsJsonAsync("/api/expenses", new
            {
                Date = System.DateTime.UtcNow,
                Title = "Protected Expense",
                Amount = 75m,
                Category = ExpenseCategories.General
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var expenseId = created.GetProperty("id").GetInt32();

            var cashierClient = CreateClient(RoleNames.Cashier, "expenses.cashier-delete");
            var response = await cashierClient.DeleteAsync($"/api/expenses/{expenseId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteExpense_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "expenses.admin-delete-missing");

            var response = await client.DeleteAsync("/api/expenses/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
