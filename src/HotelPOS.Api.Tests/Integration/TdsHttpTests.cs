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
    /// Exercises TdsController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed TdsService) — previously only reachable via
    /// mocked-service controller tests. No TDS config/slab data is seeded, so an arbitrary
    /// financial year is always "fresh".
    /// </summary>
    public class TdsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public TdsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "tds.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private static object ValidRuleSetPayload() => new
        {
            StandardDeduction = 75000m,
            RebateIncomeLimit = 700000m,
            CessRatePercent = 4m,
            Slabs = new[]
            {
                new { IncomeFrom = 0m, IncomeTo = (decimal?)null, RatePercent = 10m, DisplayOrder = 0 }
            }
        };

        [Fact]
        public async Task GetFinancialYears_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/tds/financial-years");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFinancialYears_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "tds.cashier");

            var response = await client.GetAsync("/api/tds/financial-years");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetRuleSet_UnknownYear_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "tds.admin-get-missing");

            var response = await client.GetAsync("/api/tds/rule-sets/2050");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task SaveRuleSet_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "tds.cashier-save");

            var response = await client.PutAsJsonAsync("/api/tds/rule-sets/2051", ValidRuleSetPayload());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SaveRuleSet_AdminToken_PersistsRetrievableViaGet()
        {
            var client = CreateClient(RoleNames.Admin, "tds.admin-save");

            var saveResponse = await client.PutAsJsonAsync("/api/tds/rule-sets/2052", ValidRuleSetPayload());
            Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

            var getResponse = await client.GetAsync("/api/tds/rule-sets/2052");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task SaveRuleSet_NonContiguousSlabs_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "tds.admin-noncontiguous");

            var response = await client.PutAsJsonAsync("/api/tds/rule-sets/2053", new
            {
                StandardDeduction = 75000m,
                RebateIncomeLimit = 700000m,
                CessRatePercent = 4m,
                Slabs = new[]
                {
                    new { IncomeFrom = 0m, IncomeTo = (decimal?)300000m, RatePercent = 5m, DisplayOrder = 0 },
                    // Gap between 300000 and 800000 - not contiguous with the band above.
                    new { IncomeFrom = 800000m, IncomeTo = (decimal?)null, RatePercent = 20m, DisplayOrder = 1 }
                }
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SaveRuleSet_NoSlabs_ReturnsBadRequest()
        {
            var client = CreateClient(RoleNames.Admin, "tds.admin-no-slabs");

            var response = await client.PutAsJsonAsync("/api/tds/rule-sets/2054", new
            {
                StandardDeduction = 75000m,
                RebateIncomeLimit = 700000m,
                CessRatePercent = 4m,
                Slabs = System.Array.Empty<object>()
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteRuleSet_AdminToken_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "tds.admin-delete");
            var save = await client.PutAsJsonAsync("/api/tds/rule-sets/2055", ValidRuleSetPayload());
            Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);

            var response = await client.DeleteAsync("/api/tds/rule-sets/2055");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
