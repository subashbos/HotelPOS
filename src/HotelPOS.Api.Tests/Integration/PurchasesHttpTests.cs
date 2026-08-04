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
    /// Exercises PurchasesController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed PurchaseService) — previously only reachable via
    /// mocked-service controller tests. SupplierId=1 references the seeded "Metro Wholesalers"
    /// supplier. PurchaseItem.ItemId is a real required FK to Items (via the Item? Item
    /// navigation property) enforced by SQLite, and the Items table starts empty, so every
    /// purchase-creating test seeds a real Item first rather than using an arbitrary ItemId.
    /// </summary>
    public class PurchasesHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PurchasesHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "purchases.test.user")
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

        private static object ValidPurchasePayload(string invoiceNumber, int itemId) => new
        {
            SupplierId = 1,
            InvoiceNumber = invoiceNumber,
            PurchaseDate = System.DateTime.UtcNow,
            PaymentType = PaymentModes.Cash,
            Items = new[]
            {
                new { ItemId = itemId, ItemName = "Test Ingredient", Quantity = 10, UnitPrice = 20m, TaxPercentage = 5m, Discount = 0m }
            }
        };

        [Fact]
        public async Task GetPurchases_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/purchases");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreatePurchase_CashierToken_ReturnsForbidden()
        {
            var itemId = await SeedItemAsync("Cashier Test Ingredient");
            var client = CreateClient(RoleNames.Cashier, "purchases.cashier-create");

            var response = await client.PostAsJsonAsync("/api/purchases", ValidPurchasePayload("INV-CASHIER-1", itemId));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreatePurchase_AdminToken_ReturnsCreated()
        {
            var itemId = await SeedItemAsync("Admin Test Ingredient");
            var client = CreateClient(RoleNames.Admin, "purchases.admin-create");

            var response = await client.PostAsJsonAsync("/api/purchases", ValidPurchasePayload("INV-ADMIN-1", itemId));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreatePurchase_NoItems_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "purchases.admin-no-items");

            var response = await client.PostAsJsonAsync("/api/purchases", new
            {
                SupplierId = 1,
                InvoiceNumber = "INV-NO-ITEMS",
                PurchaseDate = System.DateTime.UtcNow,
                PaymentType = PaymentModes.Cash,
                Items = System.Array.Empty<object>()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreatePurchase_InvalidSupplierId_ReturnsBadRequest()
        {
            var itemId = await SeedItemAsync("Invalid Supplier Test Ingredient");
            var client = CreateClient(RoleNames.Admin, "purchases.admin-invalid-supplier");

            var response = await client.PostAsJsonAsync("/api/purchases", new
            {
                SupplierId = 0,
                InvoiceNumber = "INV-NO-SUPPLIER",
                PurchaseDate = System.DateTime.UtcNow,
                PaymentType = PaymentModes.Cash,
                Items = new[] { new { ItemId = itemId, ItemName = "X", Quantity = 1, UnitPrice = 10m, TaxPercentage = 0m, Discount = 0m } }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetPurchase_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "purchases.admin-get-missing");

            var response = await client.GetAsync("/api/purchases/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePurchase_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "purchases.admin-update-missing");

            // UpdatePurchaseCommandHandler checks purchase existence before touching any items,
            // so this itemId is never dereferenced - no need to seed a real Item for this case.
            var response = await client.PutAsJsonAsync("/api/purchases/999999", ValidPurchasePayload("INV-UPDATE-MISSING", itemId: 1));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeletePurchase_UnknownId_IsIdempotent_ReturnsNoContent()
        {
            // DeletePurchaseCommandHandler treats a missing purchase as an idempotent no-op
            // (same convention as OrderService.DeleteOrderInternalAsync), not a 404.
            var client = CreateClient(RoleNames.Admin, "purchases.admin-delete-missing");

            var response = await client.DeleteAsync("/api/purchases/999999");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task GetSuppliers_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "purchases.admin-suppliers");

            var response = await client.GetAsync("/api/purchases/suppliers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
