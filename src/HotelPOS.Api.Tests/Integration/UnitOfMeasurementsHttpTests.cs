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
    /// Exercises UnitOfMeasurementsController through the real HTTP pipeline (routing, model
    /// binding, JWT auth/RBAC, and the mediator-backed UnitOfMeasurementService) — previously
    /// only reachable via mocked-service controller tests.
    /// </summary>
    public class UnitOfMeasurementsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UnitOfMeasurementsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "units.test.user")
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
        public async Task GetUnitOfMeasurements_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/unitofmeasurements");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUnitOfMeasurements_CashierToken_ReturnsOk()
        {
            // GET has no service-level permission check, so any authenticated caller can list units.
            var client = CreateClient(RoleNames.Cashier, "units.cashier-list");

            var response = await client.GetAsync("/api/unitofmeasurements");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "units.cashier-create");

            var response = await client.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "Should Not Save" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_AdminToken_ReturnsCreated()
        {
            var client = CreateClient(RoleNames.Admin, "units.admin-create");

            var response = await client.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "Sachet", DisplayOrder = 5 });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_DuplicateName_ReturnsConflict()
        {
            // "Sack" is not part of the seeded default unit list (Pcs, Kg, Gram, Litre, Ml,
            // Plate, Box, Packet, Bottle, Dozen) — using a seeded name here would make this test
            // fail on its own first POST rather than exercising the intended duplicate check.
            var client = CreateClient(RoleNames.Admin, "units.admin-dupe");
            var first = await client.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "Sack" });
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "Sack" });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task CreateUnitOfMeasurement_EmptyName_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "units.admin-empty-name");

            var response = await client.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_EmptyName_ReturnsBadRequest()
        {
            // UpdateUnitOfMeasurementCommand is a void IRequest, unlike
            // CreateUnitOfMeasurementCommand (IRequest<int>, already correctly validated) - the
            // same shape documented as bypassing ValidationBehavior for Save/UpdatePurchaseCommand
            // and UpdateOrderCommand (QA_REVIEW_AND_TEST_GAPS.md item 8). UnitOfMeasurementService
            // already held the right validator for its legacy path; it just wasn't reached before
            // the mediator branch returned. Now validates directly for both paths.
            var adminClient = CreateClient(RoleNames.Admin, "units.admin-update-empty-name");
            var create = await adminClient.PostAsJsonAsync("/api/unitofmeasurements", new { Name = "Crate" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var created = await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var unitId = created.GetProperty("id").GetInt32();

            var response = await adminClient.PutAsJsonAsync($"/api/unitofmeasurements/{unitId}", new { Name = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUnitOfMeasurement_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "units.admin-update-missing");

            var response = await client.PutAsJsonAsync("/api/unitofmeasurements/999999", new { Name = "Ghost Unit" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
