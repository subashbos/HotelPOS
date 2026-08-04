using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises CashSessionsController through the real HTTP pipeline (routing, model binding,
    /// JWT auth, and the mediator-backed CashService) — previously only reachable via
    /// mocked-service controller tests, which can't catch routing/serialization/DI wiring bugs.
    ///
    /// The "current session" is a single global cash drawer, not scoped per user, so every test
    /// force-closes any leftover open session first — xUnit doesn't guarantee method execution
    /// order within a class, and a session left open by one test would otherwise leak into the
    /// next.
    /// </summary>
    public class CashSessionsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CashSessionsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "cash.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task EnsureNoActiveSessionAsync()
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

        [Fact]
        public async Task GetCurrentSession_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/cashsessions/current");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentSession_NoActiveSession_ReturnsNotFound()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.no-session");

            var response = await client.GetAsync("/api/cashsessions/current");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task OpenSession_ValidRequest_PersistsSessionRetrievableViaGetCurrent()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.open");

            var openResponse = await client.PostAsJsonAsync("/api/cashsessions/open", new { OpeningBalance = 500m });
            Assert.Equal(HttpStatusCode.OK, openResponse.StatusCode);

            var currentResponse = await client.GetAsync("/api/cashsessions/current");
            Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        }

        [Fact]
        public async Task OpenSession_SessionAlreadyOpen_ReturnsConflict()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.double-open");
            var first = await client.PostAsJsonAsync("/api/cashsessions/open", new { OpeningBalance = 200m });
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/cashsessions/open", new { OpeningBalance = 300m });

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact]
        public async Task OpenSession_NegativeOpeningBalance_ReturnsBadRequest()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.negative-open");

            var response = await client.PostAsJsonAsync("/api/cashsessions/open", new { OpeningBalance = -50m });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CloseSession_NoActiveSession_ReturnsConflict()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.close-none");

            var response = await client.PostAsJsonAsync("/api/cashsessions/close", new { ActualCash = 100m });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CloseSession_AfterOpen_ReturnsNoContentAndClearsCurrent()
        {
            await EnsureNoActiveSessionAsync();
            var client = CreateClient(RoleNames.Cashier, "cash.open-close");
            var open = await client.PostAsJsonAsync("/api/cashsessions/open", new { OpeningBalance = 500m });
            Assert.Equal(HttpStatusCode.OK, open.StatusCode);

            var close = await client.PostAsJsonAsync("/api/cashsessions/close", new { ActualCash = 500m, Notes = "End of shift" });
            Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);

            var current = await client.GetAsync("/api/cashsessions/current");
            Assert.Equal(HttpStatusCode.NotFound, current.StatusCode);
        }
    }
}
