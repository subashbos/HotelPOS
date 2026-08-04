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
    /// Exercises ReservationsController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed ReservationService). Reservation.TableId is a real
    /// required FK to Tables, so every reservation-creating test seeds a real Table first, same
    /// convention as PurchasesHttpTests/EstimationsHttpTests seeding a real Item.
    /// </summary>
    public class ReservationsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ReservationsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "reservations.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        private async Task<int> SeedTableAsync(string name, int capacity = 4)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var table = new Table { Number = new Random().Next(1000, 999999), Name = name, Capacity = capacity, IsActive = true };
            context.Tables.Add(table);
            await context.SaveChangesAsync();
            return table.Id;
        }

        private static object ValidReservationPayload(int tableId, DateTime date, string start = "19:00:00", string end = "20:00:00") => new
        {
            TableId = tableId,
            ReservationDate = date,
            StartTime = start,
            EndTime = end,
            PartySize = 2
        };

        [Fact]
        public async Task GetReservations_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/reservations");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_CashierToken_ReturnsForbidden()
        {
            var tableId = await SeedTableAsync("Cashier Test Table");
            var client = CreateClient(RoleNames.Cashier, "reservations.cashier-create");

            var response = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, new DateTime(2026, 9, 1)));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_AdminToken_ReturnsCreatedWithReservedStatus()
        {
            var tableId = await SeedTableAsync("Admin Test Table");
            var client = CreateClient(RoleNames.Admin, "reservations.admin-create");

            var response = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, new DateTime(2026, 9, 2)));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("Reserved", body.GetProperty("status").GetString());
        }

        [Fact]
        public async Task CreateReservation_OverlappingSlot_ReturnsBadRequest()
        {
            var tableId = await SeedTableAsync("Overlap Test Table");
            var client = CreateClient(RoleNames.Admin, "reservations.admin-overlap");
            var date = new DateTime(2026, 9, 3);

            var first = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, date, "19:00:00", "20:00:00"));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            var second = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, date, "19:30:00", "20:30:00"));

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }

        [Fact]
        public async Task CreateReservation_PartySizeExceedsCapacity_ReturnsBadRequest()
        {
            var tableId = await SeedTableAsync("Small Table", capacity: 2);
            var client = CreateClient(RoleNames.Admin, "reservations.admin-capacity");

            var response = await client.PostAsJsonAsync("/api/reservations", new
            {
                TableId = tableId,
                ReservationDate = new DateTime(2026, 9, 4),
                StartTime = "19:00:00",
                EndTime = "20:00:00",
                PartySize = 8
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetReservation_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "reservations.admin-get-missing");

            var response = await client.GetAsync("/api/reservations/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteReservation_UnknownId_IsIdempotent_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "reservations.admin-delete-missing");

            var response = await client.DeleteAsync("/api/reservations/999999");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_UnknownId_ReturnsNotFound()
        {
            var client = CreateClient(RoleNames.Admin, "reservations.admin-status-missing");

            var response = await client.PatchAsync("/api/reservations/999999/status",
                JsonContent.Create(new { Status = ReservationStatuses.CheckedIn }));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_ReservedToCheckedIn_ReturnsNoContent()
        {
            var tableId = await SeedTableAsync("Status Test Table");
            var client = CreateClient(RoleNames.Admin, "reservations.admin-status-checkin");

            var createResponse = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, new DateTime(2026, 9, 5)));
            var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            var response = await client.PatchAsync($"/api/reservations/{id}/status",
                JsonContent.Create(new { Status = ReservationStatuses.CheckedIn }));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task ChangeStatus_ReservedDirectlyToCompleted_ReturnsBadRequest()
        {
            var tableId = await SeedTableAsync("Illegal Transition Table");
            var client = CreateClient(RoleNames.Admin, "reservations.admin-status-illegal");

            var createResponse = await client.PostAsJsonAsync("/api/reservations", ValidReservationPayload(tableId, new DateTime(2026, 9, 6)));
            var created = await createResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var id = created.GetProperty("id").GetInt32();

            // Reserved -> Completed skips the CheckedIn step and must be rejected.
            var response = await client.PatchAsync($"/api/reservations/{id}/status",
                JsonContent.Create(new { Status = ReservationStatuses.Completed }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
