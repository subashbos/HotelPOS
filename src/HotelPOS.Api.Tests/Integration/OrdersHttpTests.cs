using System;
using System.Linq;
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
    /// Exercises OrdersController through the real HTTP pipeline (routing, JWT auth/RBAC, the
    /// mediator-backed OrderService, and the real SQLite-backed DbContext) — previously only
    /// reachable via mocked-service controller tests.
    ///
    /// Two permission quirks worth locking in: (1) Creating an order and processing a partial
    /// payment gate on the "Billing" module, which the seeded Cashier-role *fallback* grants —
    /// but voiding, refunding, and updating an already-placed order gate on the stricter
    /// "OrderManagement" module, which that same fallback does NOT grant, so a bare Cashier JWT
    /// can create an order yet gets 403 trying to void/refund/edit it. (2) OrderService itself
    /// enforces that a cash drawer session is open before letting anyone create an order (a
    /// server-side rule, not just a WPF-client convenience), so tests that create orders seed an
    /// open CashSession row directly rather than going through CashSessionsController.
    ///
    /// Also note: CreateOrder returns 200 OK with a bare int order id, not 201 Created — unlike
    /// most other create endpoints in this codebase — and there is no GET-by-id route on this
    /// controller, so effects of Void/Refund/Update are verified by reading the order back
    /// directly from the DbContext.
    /// </summary>
    public class OrdersHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public OrdersHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "orders.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<int> SeedItemAsync(string name, decimal price = 100m)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            var unit = new UnitOfMeasurement { Name = $"Unit-{Guid.NewGuid():N}" };
            context.UnitOfMeasurements.Add(unit);
            await context.SaveChangesAsync();

            var item = new Item { Name = name, Price = price, TaxPercentage = 5, UnitId = unit.Id, TrackInventory = false };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            return item.Id;
        }

        private async Task SeedOpenCashSessionAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var hasOpen = await context.CashSessions.AnyAsync(s => s.Status == CashSessionStatuses.Open);
            if (hasOpen) return;

            context.CashSessions.Add(new CashSession
            {
                OpenedAt = DateTime.UtcNow,
                OpeningBalance = 1000m,
                OpenedBy = "orders.test.setup",
                Status = CashSessionStatuses.Open
            });
            await context.SaveChangesAsync();
        }

        private async Task CloseAllOpenCashSessionsAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var open = await context.CashSessions.Where(s => s.Status == CashSessionStatuses.Open).ToListAsync();
            if (open.Count == 0) return;

            foreach (var session in open)
            {
                session.Status = CashSessionStatuses.Closed;
                session.ClosedAt = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
        }

        private async Task<Order?> GetOrderFromDbAsync(int orderId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            return await context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private static object OrderPayload(int itemId, string itemName, int qty = 1, int tableNumber = 5) => new
        {
            Items = new[] { new { ItemId = itemId, ItemName = itemName, Quantity = qty } },
            TableNumber = tableNumber,
            Discount = 0m,
            PaymentMode = PaymentModes.Cash,
            OrderType = OrderTypes.DineIn
        };

        private async Task<int> CreateOrderAsync(HttpClient client, int itemId, string itemName, int qty = 1)
        {
            var response = await client.PostAsJsonAsync("/api/orders", OrderPayload(itemId, itemName, qty));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadFromJsonAsync<int>();
        }

        [Fact]
        public async Task GetOrders_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/orders");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOrders_CashierFallback_ReturnsOk()
        {
            // GetOrdersQueryHandler has no permission check at all, so even a bare Cashier JWT works.
            var client = CreateClient(RoleNames.Cashier, "orders.cashier-list");

            var response = await client.GetAsync("/api/orders");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_CashierFallback_ReturnsOk()
        {
            // Create gates on Billing, which the Cashier-role fallback grants.
            await SeedOpenCashSessionAsync();
            var itemId = await SeedItemAsync("Orders Cashier Create Item");
            var client = CreateClient(RoleNames.Cashier, "orders.cashier-create");

            var response = await client.PostAsJsonAsync("/api/orders", OrderPayload(itemId, "Orders Cashier Create Item"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var orderId = await response.Content.ReadFromJsonAsync<int>();
            Assert.True(orderId > 0);
        }

        [Fact]
        public async Task CreateOrder_NoOpenCashSession_ReturnsBadRequest()
        {
            await CloseAllOpenCashSessionsAsync();
            var itemId = await SeedItemAsync("Orders No Session Item");
            var client = CreateClient(RoleNames.Admin, "orders.admin-no-session");

            var response = await client.PostAsJsonAsync("/api/orders", OrderPayload(itemId, "Orders No Session Item"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_EmptyItems_ReturnsBadRequest()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-empty-items");

            var response = await client.PostAsJsonAsync("/api/orders", new
            {
                Items = Array.Empty<object>(),
                TableNumber = 5,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_UnknownItem_ReturnsBadRequest()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-unknown-item");

            var response = await client.PostAsJsonAsync("/api/orders", OrderPayload(999999, "Ghost Item"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_CashierFallback_ReturnsForbidden()
        {
            // Void gates on OrderManagement, which the Cashier fallback does NOT grant, even
            // though the same fallback let Cashier create the order in the first place.
            await SeedOpenCashSessionAsync();
            var adminClient = CreateClient(RoleNames.Admin, "orders.void-setup-admin");
            var itemId = await SeedItemAsync("Orders Void Setup Item");
            var orderId = await CreateOrderAsync(adminClient, itemId, "Orders Void Setup Item");

            var cashierClient = CreateClient(RoleNames.Cashier, "orders.cashier-void");
            var response = await cashierClient.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "Cashier attempt" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_AdminToken_ReturnsNoContentAndZeroesTotals()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-void");
            var itemId = await SeedItemAsync("Orders Void Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Void Item");

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "Customer cancelled" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var order = await GetOrderFromDbAsync(orderId);
            Assert.NotNull(order);
            Assert.Equal(0m, order!.TotalAmount);
            Assert.Equal(0m, order.Subtotal);
            Assert.Equal(OrderStatuses.Void, order.Status);
            Assert.Equal("Customer cancelled", order.VoidReason);
        }

        [Fact]
        public async Task VoidOrder_MissingReason_ReturnsBadRequest()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-void-no-reason");
            var itemId = await SeedItemAsync("Orders Void No Reason Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Void No Reason Item");

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_CalledTwice_SecondCallReturnsBadRequest()
        {
            // VoidOrderInternalAsync guards against voiding an already-void order by checking
            // the freshly-read order's Status, which requires UpdateAsync to have actually
            // persisted Status from the first void call.
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-double-void");
            var itemId = await SeedItemAsync("Orders Double Void Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Double Void Item");
            var first = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "First void" });
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "Second void" });

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "orders.admin-void-missing");

            var response = await client.PostAsJsonAsync("/api/orders/999999/void", new { Reason = "Doesn't exist" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task RefundOrder_AdminToken_ReturnsNoContentAndReducesTotal()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-refund");
            var itemId = await SeedItemAsync("Orders Refund Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Refund Item", qty: 2);
            var before = await GetOrderFromDbAsync(orderId);
            Assert.NotNull(before);
            var totalBeforeRefund = before!.TotalAmount;

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/refund", new
            {
                Items = new[] { new { ItemId = itemId, QuantityToRefund = 1 } },
                Reason = "Customer sent one back"
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var order = await GetOrderFromDbAsync(orderId);
            Assert.NotNull(order);
            Assert.True(order!.TotalAmount < totalBeforeRefund);
            Assert.Equal(1, order.Items.Single().Quantity);
            Assert.Equal(OrderStatuses.PartiallyRefunded, order.Status);
            Assert.Equal("Customer sent one back", order.RefundReason);
        }

        [Fact]
        public async Task RefundOrder_CashierFallback_ReturnsForbidden()
        {
            await SeedOpenCashSessionAsync();
            var adminClient = CreateClient(RoleNames.Admin, "orders.refund-setup-admin");
            var itemId = await SeedItemAsync("Orders Refund Forbidden Item");
            var orderId = await CreateOrderAsync(adminClient, itemId, "Orders Refund Forbidden Item");

            var cashierClient = CreateClient(RoleNames.Cashier, "orders.cashier-refund");
            var response = await cashierClient.PostAsJsonAsync($"/api/orders/{orderId}/refund", new
            {
                Items = new[] { new { ItemId = itemId, QuantityToRefund = 1 } },
                Reason = "Cashier attempt"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task RefundOrder_EmptyItems_ReturnsBadRequest()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-refund-empty");
            var itemId = await SeedItemAsync("Orders Refund Empty Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Refund Empty Item");

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/refund", new
            {
                Items = Array.Empty<object>(),
                Reason = "No items"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ProcessPartialPayment_CashierFallback_ReturnsNoContent()
        {
            // Partial payment gates on Billing, same as create, so the Cashier fallback covers it.
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "orders.cashier-payment");
            var itemId = await SeedItemAsync("Orders Payment Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Payment Item");

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/payment", new { Cash = 10m, Card = 0m, Upi = 0m });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ProcessPartialPayment_AllZero_ReturnsBadRequest()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-zero-payment");
            var itemId = await SeedItemAsync("Orders Zero Payment Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Zero Payment Item");

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/payment", new { Cash = 0m, Card = 0m, Upi = 0m });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrder_AdminToken_ReturnsNoContent()
        {
            await SeedOpenCashSessionAsync();
            var client = CreateClient(RoleNames.Admin, "orders.admin-update");
            var itemId = await SeedItemAsync("Orders Update Item");
            var orderId = await CreateOrderAsync(client, itemId, "Orders Update Item", qty: 1);

            var response = await client.PutAsJsonAsync($"/api/orders/{orderId}", new
            {
                Items = new[] { new { ItemId = itemId, ItemName = "Orders Update Item", Quantity = 2 } },
                TableNumber = 5,
                Discount = 0m,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var order = await GetOrderFromDbAsync(orderId);
            Assert.NotNull(order);
            Assert.Equal(2, order!.Items.Single().Quantity);
        }

        [Fact]
        public async Task UpdateOrder_CashierFallback_ReturnsForbidden()
        {
            await SeedOpenCashSessionAsync();
            var adminClient = CreateClient(RoleNames.Admin, "orders.update-setup-admin");
            var itemId = await SeedItemAsync("Orders Update Forbidden Item");
            var orderId = await CreateOrderAsync(adminClient, itemId, "Orders Update Forbidden Item");

            var cashierClient = CreateClient(RoleNames.Cashier, "orders.cashier-update");
            var response = await cashierClient.PutAsJsonAsync($"/api/orders/{orderId}", new
            {
                Items = new[] { new { ItemId = itemId, ItemName = "Orders Update Forbidden Item", Quantity = 3 } },
                TableNumber = 5,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrder_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "orders.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/orders/999999", new
            {
                Items = new[] { new { ItemId = 1, ItemName = "Ghost", Quantity = 1 } },
                TableNumber = 5,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
