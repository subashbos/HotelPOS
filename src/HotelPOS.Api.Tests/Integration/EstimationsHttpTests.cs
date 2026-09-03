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
    /// Exercises EstimationsController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed EstimationService). Successful conversion-to-order is
    /// covered at the handler level (ConvertEstimationToOrderCommandHandlerTests) instead of here,
    /// since it additionally requires an open CashSession - this file sticks to the CRUD surface
    /// plus the convert-to-order rejection paths that don't depend on that precondition.
    /// EstimationItem.ItemId is a real required FK to Items, so every estimation-creating test
    /// seeds a real Item first, same convention as PurchasesHttpTests.
    /// </summary>
    public class EstimationsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public EstimationsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "estimations.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<int> SeedItemAsync(string name)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var item = new Item { Name = name, Price = 20m, UnitId = 1 };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            return item.Id;
        }

        private static object ValidEstimationPayload(string estimationNumber, int itemId) => new
        {
            EstimationNumber = estimationNumber,
            EstimationDate = System.DateTime.UtcNow,
            Items = new[]
            {
                new { ItemId = itemId, ItemName = "Test Item", Quantity = 2, UnitPrice = 500m, TaxPercentage = 5m, Discount = 0m }
            }
        };

        [Fact]
        public async Task GetEstimations_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/estimations");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateEstimation_CashierToken_ReturnsForbidden()
        {
            var itemId = await SeedItemAsync("Cashier Test Item");
            var client = CreateClient(RoleNames.Cashier, "estimations.cashier-create");

            var response = await client.PostAsJsonAsync("/api/estimations", ValidEstimationPayload("EST-CASHIER-1", itemId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateEstimation_AdminToken_ReturnsCreatedWithDraftStatus()
        {
            var itemId = await SeedItemAsync("Admin Test Item");
            var client = CreateClient(RoleNames.Admin, "estimations.admin-create");

            var response = await client.PostAsJsonAsync("/api/estimations", ValidEstimationPayload("EST-ADMIN-1", itemId));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("Draft", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CreateEstimation_NoItems_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "estimations.admin-no-items");

            var response = await client.PostAsJsonAsync("/api/estimations", new
            {
                EstimationNumber = "EST-NO-ITEMS",
                EstimationDate = System.DateTime.UtcNow,
                Items = System.Array.Empty<object>()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateEstimation_ZeroQuantityItem_ReturnsBadRequest()
        {
            // SaveEstimationCommandValidator rejects a zero item quantity. SaveEstimationCommand
            // is a void IRequest, the same shape documented as bypassing ValidationBehavior for
            // Save/UpdatePurchaseCommand and UpdateOrderCommand (QA_REVIEW_AND_TEST_GAPS.md item
            // 8) - EstimationService now validates directly rather than relying on that pipeline.
            var itemId = await SeedItemAsync("Zero Quantity Test Item");
            var client = CreateClient(RoleNames.Admin, "estimations.admin-zero-qty");

            var response = await client.PostAsJsonAsync("/api/estimations", new
            {
                EstimationNumber = "EST-ZERO-QTY",
                EstimationDate = System.DateTime.UtcNow,
                Items = new[]
                {
                    new { ItemId = itemId, ItemName = "Test Item", Quantity = 0, UnitPrice = 500m, TaxPercentage = 5m, Discount = 0m }
                }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetEstimation_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "estimations.admin-get-missing");

            var response = await client.GetAsync("/api/estimations/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateEstimation_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "estimations.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/estimations/999999", ValidEstimationPayload("EST-UPDATE-MISSING", itemId: 1));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteEstimation_UnknownId_IsIdempotent_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "estimations.admin-delete-missing");

            var response = await client.DeleteAsync("/api/estimations/999999");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ConvertToOrder_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "estimations.admin-convert-missing");

            var response = await client.PostAsync("/api/estimations/999999/convert-to-order", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConvertToOrder_DraftEstimation_ReturnsBadRequest()
        {
            var itemId = await SeedItemAsync("Convert Draft Test Item");
            var client = CreateClient(RoleNames.Admin, "estimations.admin-convert-draft");

            var createResponse = await client.PostAsJsonAsync("/api/estimations", ValidEstimationPayload("EST-CONVERT-DRAFT", itemId));
            var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            // A freshly created estimation is Draft, and only an Accepted estimation may be
            // converted, so this must be rejected without ever touching IOrderService.
            var response = await client.PostAsync($"/api/estimations/{id}/convert-to-order", null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
