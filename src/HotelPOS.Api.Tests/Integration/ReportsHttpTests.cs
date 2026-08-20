using System;
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
    /// Exercises ReportsController through the real HTTP pipeline (routing, JWT auth/RBAC, and
    /// both the mediator-backed IReportService and the directly-injected IBIReportService) —
    /// previously only reachable via mocked-service controller tests.
    ///
    /// Every single endpoint on this controller — GET or POST, ReportService- or
    /// BIReportService-backed — gates on the one "SalesReport" permission module. The seeded
    /// Cashier-role fallback grants only Billing/Shift, so a bare Cashier JWT is forbidden from
    /// every report endpoint without exception; only Admin (or a real DB-seeded role with
    /// SalesReport access) can reach any of them.
    /// </summary>
    public class ReportsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ReportsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "reports.test.user")
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
            var unit = new UnitOfMeasurement { Name = $"Unit-{Guid.NewGuid():N}" };
            context.UnitOfMeasurements.Add(unit);
            await context.SaveChangesAsync();

            var item = new Item { Name = name, Price = 50m, TaxPercentage = 5, UnitId = unit.Id, TrackInventory = false };
            context.Items.Add(item);
            await context.SaveChangesAsync();
            return item.Id;
        }

        [Fact]
        public async Task GetSalesReport_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/reports/sales");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSalesReport_CashierFallback_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "reports.cashier-sales");

            var response = await client.GetAsync("/api/reports/sales");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetSalesReport_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-sales");

            var response = await client.GetAsync("/api/reports/sales");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetItemReport_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-items");

            var response = await client.GetAsync("/api/reports/items");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetGstReport_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-gst");

            var response = await client.GetAsync("/api/reports/gst?from=2026-01-01&to=2026-12-31");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetGstReport_CashierFallback_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "reports.cashier-gst");

            var response = await client.GetAsync("/api/reports/gst?from=2026-01-01&to=2026-12-31");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetMonthlyChart_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-monthly-chart");

            var response = await client.GetAsync("/api/reports/monthly-chart");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPurchaseReport_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-purchases");

            var response = await client.GetAsync("/api/reports/purchases");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetMarginSummary_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-margins");

            var response = await client.GetAsync("/api/reports/margins/summary");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetWastageSummary_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-wastage-summary");

            var response = await client.GetAsync("/api/reports/wastage");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetLowStockAlerts_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-low-stock");

            var response = await client.GetAsync("/api/reports/low-stock");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetProfitAndLossReport_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-pnl");

            var response = await client.GetAsync("/api/reports/pnl");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LogWastage_CashierFallback_ReturnsForbidden()
        {
            var itemId = await SeedItemAsync("Reports Wastage Forbidden Item");
            var client = CreateClient(RoleNames.Cashier, "reports.cashier-log-wastage");

            var response = await client.PostAsJsonAsync("/api/reports/wastage", new { ItemId = itemId, Quantity = 1, Reason = "Spilled" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LogWastage_MissingReason_ReturnsBadRequest()
        {
            var itemId = await SeedItemAsync("Reports Wastage No Reason Item");
            var client = CreateClient(RoleNames.Admin, "reports.admin-log-wastage-no-reason");

            var response = await client.PostAsJsonAsync("/api/reports/wastage", new { ItemId = itemId, Quantity = 1, Reason = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LogWastage_UnknownItem_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "reports.admin-log-wastage-missing-item");

            var response = await client.PostAsJsonAsync("/api/reports/wastage", new { ItemId = 999999, Quantity = 1, Reason = "Spilled" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task LogWastage_ValidItem_ReturnsNoContent()
        {
            var itemId = await SeedItemAsync("Reports Wastage Valid Item");
            var client = CreateClient(RoleNames.Admin, "reports.admin-log-wastage-valid");

            var response = await client.PostAsJsonAsync("/api/reports/wastage", new { ItemId = itemId, Quantity = 2, Reason = "Dropped on floor" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
