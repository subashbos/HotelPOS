using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises ItemsController through the real HTTP pipeline (routing, JWT auth/RBAC, and the
    /// mediator-backed Create/Update/Delete item commands) — previously only reachable via
    /// mocked-service controller tests.
    ///
    /// Two surprises worth locking in from reading the real handlers rather than assuming: (1)
    /// duplicate name/barcode on create or update throws InvalidOperationException, which the
    /// global exception middleware maps to 400 BadRequest, NOT 409 Conflict — unlike some other
    /// modules in this codebase. (2) Deleting an item has zero protection against it being
    /// referenced by existing orders: OrderItem.ItemId is a plain int with no EF foreign-key
    /// constraint to Item (unlike Item -> UnitOfMeasurement, which *is* FK-restricted at the DB
    /// level), so deleting an item still in use by a real order succeeds with 204, leaving a
    /// dangling reference. Item.UnitId, on the other hand, IS FK-restricted, so every item here
    /// is created against a real seeded UnitOfMeasurement row.
    /// </summary>
    public class ItemsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ItemsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "items.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<int> SeedUnitAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var unit = new UnitOfMeasurement { Name = $"Unit-{Guid.NewGuid():N}" };
            context.UnitOfMeasurements.Add(unit);
            await context.SaveChangesAsync();
            return unit.Id;
        }

        private async Task<int> SeedOpenCashSessionAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var existing = await context.CashSessions.FirstOrDefaultAsync(s => s.Status == CashSessionStatuses.Open);
            if (existing != null) return existing.Id;

            var session = new CashSession
            {
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = 1000m,
                OpenedBy = "items.test.setup",
                Status = CashSessionStatuses.Open
            };
            context.CashSessions.Add(session);
            await context.SaveChangesAsync();
            return session.Id;
        }

        private async Task<object> ValidItemPayload(string name, string? barcode = null) => new
        {
            Name = name,
            Price = 150m,
            TaxPercentage = 5m,
            HsnCode = "996331",
            Barcode = barcode,
            StockQuantity = 10,
            TrackInventory = false,
            UnitId = await SeedUnitAsync()
        };

        [Fact]
        public async Task GetItems_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/items");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetItems_CashierFallback_ReturnsOk()
        {
            // GetItemsQueryHandler has no permission check at all, so even a bare Cashier JWT works.
            var client = CreateClient(RoleNames.Cashier, "items.cashier-list");

            var response = await client.GetAsync("/api/items");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetItem_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-get-missing");

            var response = await client.GetAsync("/api/items/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_CashierFallback_ReturnsForbidden()
        {
            // Create gates on the "Items" module, which the Cashier fallback does not grant.
            var client = CreateClient(RoleNames.Cashier, "items.cashier-create");

            var response = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Cashier Create Attempt"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-create");

            var response = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Admin Created"));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.True(dto.GetProperty("id").GetInt32() > 0);
        }

        [Fact]
        public async Task CreateItem_InvalidPrice_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-invalid-price");
            var unitId = await SeedUnitAsync();

            var response = await client.PostAsJsonAsync("/api/items", new
            {
                Name = "Items Invalid Price",
                Price = 0m,
                TaxPercentage = 5m,
                StockQuantity = 0,
                TrackInventory = false,
                UnitId = unitId
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_DuplicateName_ReturnsBadRequest()
        {
            // Duplicate name is InvalidOperationException in the handler, which the global
            // exception middleware maps to 400 BadRequest — not 409 Conflict.
            var client = CreateClient(RoleNames.Admin, "items.admin-dupe-name");
            var first = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Duplicate Name"));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Duplicate Name"));

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task CreateItem_DuplicateBarcode_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-dupe-barcode");
            const string barcode = "ITEMS-BARCODE-0001";
            var first = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Barcode Owner", barcode));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Barcode Claimant", barcode));

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task UpdateItem_AdminToken_ReturnsOkWithUpdatedFields()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-update");
            var create = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Update Target"));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var itemId = created.GetProperty("id").GetInt32();
            var unitId = await SeedUnitAsync();

            var response = await client.PutAsJsonAsync($"/api/items/{itemId}", new
            {
                Name = "Items Update Target Renamed",
                Price = 199m,
                TaxPercentage = 12m,
                StockQuantity = 20,
                TrackInventory = false,
                UnitId = unitId
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("Items Update Target Renamed", dto.GetProperty("name").GetString());
        }

        [Fact]
        public async Task UpdateItem_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-update-missing");
            var unitId = await SeedUnitAsync();

            var response = await client.PutAsJsonAsync("/api/items/999999", new
            {
                Name = "Ghost Item",
                Price = 100m,
                TaxPercentage = 5m,
                StockQuantity = 0,
                TrackInventory = false,
                UnitId = unitId
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_AdminToken_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-delete");
            var create = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Delete Target"));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var itemId = created.GetProperty("id").GetInt32();

            var response = await client.DeleteAsync($"/api/items/{itemId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "items.admin-delete-missing");

            var response = await client.DeleteAsync("/api/items/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_CashierFallback_ReturnsForbidden()
        {
            var adminClient = CreateClient(RoleNames.Admin, "items.delete-setup-admin");
            var create = await adminClient.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Delete Forbidden Target"));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var itemId = created.GetProperty("id").GetInt32();

            var cashierClient = CreateClient(RoleNames.Cashier, "items.cashier-delete");
            var response = await cashierClient.DeleteAsync($"/api/items/{itemId}");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_ReferencedByRealOrder_StillReturnsNoContent()
        {
            // Confirms there is no FK-level or application-level protection stopping an item
            // that's already used on a real order from being deleted.
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "items.admin-delete-referenced");
            var create = await client.PostAsJsonAsync("/api/items", await ValidItemPayload("Items Referenced By Order"));
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var itemId = created.GetProperty("id").GetInt32();

            var orderResponse = await client.PostAsJsonAsync("/api/orders", new
            {
                Items = new[] { new { ItemId = itemId, ItemName = "Items Referenced By Order", Quantity = 1 } },
                TableNumber = 7,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });
            Assert.Equal(HttpStatusCode.OK, orderResponse.StatusCode);

            var response = await client.DeleteAsync($"/api/items/{itemId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
