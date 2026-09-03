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
    /// Exercises CategoriesController through the real HTTP pipeline (routing, model binding,
    /// JWT auth/RBAC, and the mediator-backed CategoryService) — previously only reachable via
    /// mocked-service controller tests.
    /// </summary>
    public class CategoriesHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CategoriesHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "categories.test.user")
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
        public async Task GetCategories_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/categories");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "categories.cashier-create");

            var response = await client.PostAsJsonAsync("/api/categories", new { Name = "Should Not Save" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "categories.admin-create");

            var response = await client.PostAsJsonAsync("/api/categories", new { Name = "Beverages", DisplayOrder = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_DuplicateName_ReturnsConflict()
        {
            var client = CreateClient(RoleNames.Admin, "categories.admin-dupe");
            var first = await client.PostAsJsonAsync("/api/categories", new { Name = "Starters" });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/categories", new { Name = "Starters" });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateCategory_EmptyName_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "categories.admin-empty-name");

            var response = await client.PostAsJsonAsync("/api/categories", new { Name = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_EmptyName_ReturnsBadRequest()
        {
            // UpdateCategoryCommand is a void IRequest, unlike CreateCategoryCommand (IRequest<int>,
            // already correctly validated) - the same shape documented as bypassing
            // ValidationBehavior for Save/UpdatePurchaseCommand and UpdateOrderCommand
            // (QA_REVIEW_AND_TEST_GAPS.md item 8). CategoryService already held the right
            // validator for its legacy path; it just wasn't reached before the mediator branch
            // returned. Now validates directly for both paths.
            var adminClient = CreateClient(RoleNames.Admin, "categories.admin-update-empty-name");
            var create = await adminClient.PostAsJsonAsync("/api/categories", new { Name = "Snacks" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var categoryId = created.GetProperty("id").GetInt32();

            var response = await adminClient.PutAsJsonAsync($"/api/categories/{categoryId}", new { Name = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateCategory_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "categories.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/categories/999999", new { Name = "Ghost Category" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteCategory_InUse_ReturnsConflict()
        {
            var adminClient = CreateClient(RoleNames.Admin, "categories.admin-delete-inuse");
            var create = await adminClient.PostAsJsonAsync("/api/categories", new { Name = "Desserts" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var categoryId = created.GetProperty("id").GetInt32();

            var itemResponse = await adminClient.PostAsJsonAsync("/api/items", new
            {
                Name = "Ice Cream",
                Price = 5m,
                TaxPercentage = 5m,
                UnitId = 1,
                CategoryId = categoryId
            });
            Assert.Equal(HttpStatusCode.Created, itemResponse.StatusCode);

            var deleteResponse = await adminClient.DeleteAsync($"/api/categories/{categoryId}");

            Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        }
    }
}
