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
    /// Exercises the real HTTP pipeline (JWT auth + permission-module checks driven by the
    /// Role/RolePermission tables) to prove Cashier tokens can no longer mutate the menu or void
    /// orders (issue #23), and that a custom role only gains access once granted a real
    /// RolePermission row — not merely by carrying a matching JWT role-name claim.
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

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Cashier Item", Price = 10m, TaxPercentage = 5m, UnitId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Admin Item", Price = 10m, TaxPercentage = 5m, UnitId = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_ManagerToken_ReturnsCreated()
        {
            // A bare "Manager" role-name JWT claim grants nothing under the generic permission
            // system - access requires a real Role row with a granted RolePermission, exactly
            // like an admin would configure via the Roles screen.
            const string username = "manager.user";
            await SeedUserWithGrantedModuleAsync(username, "Manager", PermissionModules.Items);
            var client = CreateClient(RoleNames.Manager, username);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Manager Item", Price = 10m, TaxPercentage = 5m, UnitId = 1 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateItem_CustomRoleWithoutItemsGrant_ReturnsForbidden()
        {
            // Same custom role as above, but without the Items grant - proves access is driven
            // by the RolePermission row, not by the role simply existing or being named "Manager".
            const string username = "manager.no-items";
            await SeedUserWithGrantedModuleAsync(username, "ManagerNoItems", PermissionModules.HrAttendance);
            var client = CreateClient("ManagerNoItems", username);

            var response = await client.PostAsJsonAsync("/api/items", new { Name = "Should Not Save", Price = 10m, TaxPercentage = 5m, UnitId = 1 });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

        /// <summary>Creates a custom Role granted access to exactly one module, plus a User linked to it — the DB-driven path an admin would set up via the Roles screen.</summary>
        private async Task SeedUserWithGrantedModuleAsync(string username, string roleName, string grantedModule)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            var role = new Role { Name = roleName, Description = "Test role" };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, ModuleName = grantedModule, CanAccess = true });
            context.Users.Add(new User
            {
                Username = username,
                PasswordHash = "test-hash",
                Salt = "test-salt",
                Role = roleName,
                RoleId = role.Id
            });
            await context.SaveChangesAsync();
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
