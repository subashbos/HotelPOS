using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Proves domain exceptions raised by MediatR handlers flow through ExceptionMiddleware
    /// (not per-action try/catch) and come back as the expected ProblemDetails shape (issue #28).
    /// </summary>
    public class ExceptionHandlingTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ExceptionHandlingTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string role, string username = "test.user")
        {
            var client = _factory.CreateClient();
            var token = _factory.IssueToken(role, username);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task VoidOrder_NonexistentOrder_ReturnsNotFoundProblemDetails()
        {
            var client = CreateClient(RoleNames.Admin);

            var response = await client.PostAsJsonAsync("/api/orders/999999/void", new { Reason = "Not found test" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("detail").GetString()));
        }

        [Fact]
        public async Task VoidOrder_AlreadyVoidedOrder_ReturnsBadRequestProblemDetails()
        {
            var orderId = await SeedOrderAsync(OrderStatuses.Void);
            var client = CreateClient(RoleNames.Admin);

            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/void", new { Reason = "Already void" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("detail").GetString()));
        }

        [Fact]
        public async Task CreateItem_DuplicateName_ReturnsBadRequestProblemDetails()
        {
            var client = CreateClient(RoleNames.Admin);
            var itemPayload = new { Name = "Duplicate Item", Price = 10m, TaxPercentage = 5m };

            var first = await client.PostAsJsonAsync("/api/items", itemPayload);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/items", itemPayload);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
            Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
            Assert.Contains("already exists", doc.RootElement.GetProperty("detail").GetString());
        }

        [Fact]
        public async Task CreateOrder_EmptyItems_ReturnsValidationProblemDetailsWithErrors()
        {
            var client = CreateClient(RoleNames.Cashier);

            var response = await client.PostAsJsonAsync("/api/orders", new
            {
                Items = System.Array.Empty<object>(),
                TableNumber = 1,
                PaymentMode = PaymentModes.Cash,
                OrderType = OrderTypes.DineIn
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(400, doc.RootElement.GetProperty("status").GetInt32());
            Assert.True(doc.RootElement.TryGetProperty("errors", out _));
        }

        private async Task<int> SeedOrderAsync(string status)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            var order = new Order
            {
                TableNumber = 1,
                Status = status,
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
