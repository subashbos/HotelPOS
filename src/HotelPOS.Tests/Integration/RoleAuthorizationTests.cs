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
    /// Exercises the real HTTP pipeline (JWT auth + [Authorize(Roles=...)]) to prove
    /// Cashier tokens can no longer mutate the menu or void orders (issue #23).
    /// </summary>
    public class RoleAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RoleAuthorizationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "test.user")
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
        public async Task CreateItem_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Test Item", Price = 10m, TaxPercentage = 5m });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Cashier Item", Price = 10m, TaxPercentage = 5m });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Admin Item", Price = 10m, TaxPercentage = 5m });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_ManagerToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Manager);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Manager Item", Price = 10m, TaxPercentage = 5m });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetItems_CashierToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Cashier);

            var response = await client.GetAsync("/api/items");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.PostAsJsonAsync("/api/orders/1/void", new { Reason = "Test" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier);

            var response = await client.PostAsJsonAsync("/api/orders/1/void", new { Reason = "Test" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VoidOrder_AdminToken_VoidsExistingOrder()
        {
            var orderId = await SeedPaidOrderAsync();
            var client = CreateClient(RoleNames.Admin);

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "Admin void" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        private async Task<int> SeedPaidOrderAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            var order = new Order
            {
                TableNumber = 1,
                Status = OrderStatuses.Paid,
                PaymentMode = PaymentModes.Cash,
                TotalAmount = 100m,
                AmountPaid = 100m,
                CashPaid = 100m
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order.Id;
        }
    }
}
