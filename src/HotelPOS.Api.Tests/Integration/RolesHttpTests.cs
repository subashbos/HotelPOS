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
    /// Exercises RolesController's own CRUD through the real HTTP pipeline — RoleAuthorizationTests.cs
    /// already covers how Role/RolePermission rows drive permission enforcement elsewhere (Items,
    /// Orders), but not the Roles endpoints themselves (create/read/delete a role, update its
    /// permissions).
    /// </summary>
    public class RolesHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public RolesHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "roles.test.user")
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
        public async Task GetRoles_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/roles");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetRoles_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "roles.cashier");

            var response = await client.GetAsync("/api/roles");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetRoles_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "roles.admin-list");

            var response = await client.GetAsync("/api/roles");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetRole_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "roles.admin-get-missing");

            var response = await client.GetAsync("/api/roles/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateRole_AdminToken_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "roles.admin-create");

            var response = await client.PostAsJsonAsync("/api/roles", new { Name = "Shift Supervisor", Description = "Custom role" });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task CreateRole_DuplicateName_ReturnsConflict()
        {
            var client = CreateClient(RoleNames.Admin, "roles.admin-dupe");
            var first = await client.PostAsJsonAsync("/api/roles", new { Name = "Night Auditor" });
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/roles", new { Name = "Night Auditor" });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateRole_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "roles.cashier-create");

            var response = await client.PostAsJsonAsync("/api/roles", new { Name = "Should Not Save" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePermissions_GrantsModuleAccess_ReflectedInSubsequentGet()
        {
            // Uses the seeded Cashier role (Id 2) — grant it the Items module, then read it back.
            var client = CreateClient(RoleNames.Admin, "roles.admin-update-perms");

            var response = await client.PutAsJsonAsync("/api/roles/2/permissions", new[]
            {
                new { RoleId = 2, ModuleName = PermissionModules.Items, CanAccess = true }
            });
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var getResponse = await client.GetAsync("/api/roles/2");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var role = await getResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var itemsPermission = role.GetProperty("permissions").EnumerateArray()
                .First(p => p.GetProperty("moduleName").GetString() == PermissionModules.Items);
            Assert.True(itemsPermission.GetProperty("canAccess").GetBoolean());
        }

        [Fact]
        public async Task DeleteRole_UnknownId_IsIdempotent_ReturnsNoContent()
        {
            // RoleRepository.DeleteRoleAsync silently no-ops for a missing role rather than
            // throwing - the controller's KeyNotFoundException catch is unreachable via this path.
            var client = CreateClient(RoleNames.Admin, "roles.admin-delete-missing");

            var response = await client.DeleteAsync("/api/roles/999999");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteRole_AdminToken_RemovesCreatedRole()
        {
            var client = CreateClient(RoleNames.Admin, "roles.admin-delete");
            var create = await client.PostAsJsonAsync("/api/roles", new { Name = "Temp Role To Delete" });
            Assert.Equal(HttpStatusCode.NoContent, create.StatusCode);

            var list = await client.GetAsync("/api/roles");
            var roles = await list.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var createdId = roles.EnumerateArray().First(r => r.GetProperty("name").GetString() == "Temp Role To Delete").GetProperty("id").GetInt32();

            var response = await client.DeleteAsync($"/api/roles/{createdId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
