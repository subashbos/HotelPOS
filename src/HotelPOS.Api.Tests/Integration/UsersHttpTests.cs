using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises UsersController through the real HTTP pipeline (routing, model binding, JWT
    /// auth/RBAC, and the mediator-backed UserService) — previously only reachable via
    /// mocked-service controller tests.
    /// </summary>
    public class UsersHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public UsersHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username, int userId = 1)
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username, userId);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        /// <summary>Seeds a User row directly (bypassing the API) with a real PBKDF2 hash, returning its ID.</summary>
        private async Task<int> SeedUserAsync(string username, string password, string role = RoleNames.Cashier)
        {
            var authService = new AuthService(Mock.Of<IUserRepository>(), Mock.Of<ILoginLockoutRepository>());
            var (hash, salt) = authService.HashPassword(password);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var user = new User { Username = username, PasswordHash = hash, Salt = salt, Role = role };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user.Id;
        }

        [Fact]
        public async Task GetUsers_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null, username: "n/a");

            var response = await client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "users.cashier-list");

            var response = await client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_AdminToken_ReturnsOk()
        {
            var client = CreateClient(RoleNames.Admin, "users.admin-list");

            var response = await client.GetAsync("/api/users");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_AdminToken_CreatesUser_ReturnsNoContent()
        {
            var client = CreateClient(RoleNames.Admin, "users.admin-create");

            var response = await client.PostAsJsonAsync("/api/users", new
            {
                Username = "brand.new.user",
                Password = "ValidPass123!",
                Role = RoleNames.Cashier,
                RoleId = 2
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            Assert.True(await context.Users.AnyAsync(u => u.Username == "brand.new.user"));
        }

        [Fact]
        public async Task CreateUser_DuplicateUsername_ReturnsBadRequest()
        {
            await SeedUserAsync("dupe.user", "ValidPass123!");
            var client = CreateClient(RoleNames.Admin, "users.admin-dupe");

            var response = await client.PostAsJsonAsync("/api/users", new
            {
                Username = "dupe.user",
                Password = "AnotherPass123!",
                Role = RoleNames.Cashier,
                RoleId = 2
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_SelfDelete_ReturnsBadRequest()
        {
            var selfId = await SeedUserAsync("users.self-delete", "ValidPass123!", RoleNames.Admin);
            var client = CreateClient(RoleNames.Admin, "users.self-delete", userId: selfId);

            var response = await client.DeleteAsync($"/api/users/{selfId}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_SelfServiceWithWrongCurrentPassword_ReturnsBadRequest()
        {
            var userId = await SeedUserAsync("users.reset-self", "CorrectPass1!");
            // Self-service: the caller's JWT subject must equal the target user ID.
            var client = CreateClient(RoleNames.Cashier, "users.reset-self", userId: userId);

            var response = await client.PostAsJsonAsync($"/api/users/{userId}/reset-password", new
            {
                NewPassword = "NewPass123!",
                CurrentPassword = "WrongPass1!"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_AdminResetsAnotherUser_ReturnsNoContent()
        {
            var targetId = await SeedUserAsync("users.reset-target", "OldPass123!");
            var client = CreateClient(RoleNames.Admin, "users.reset-admin", userId: 999);

            var response = await client.PostAsJsonAsync($"/api/users/{targetId}/reset-password", new
            {
                NewPassword = "BrandNewPass123!"
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
