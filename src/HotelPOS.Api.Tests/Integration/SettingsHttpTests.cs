using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HotelPOS.Application.DTOs.Setting;
using HotelPOS.Domain.Common.Constants;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Exercises SettingsController through the real HTTP pipeline. GetSettings has permission-
    /// conditional field visibility (SMTP/backup/tax fields only for callers with the Settings
    /// permission) that a mocked-service controller test can't meaningfully verify end-to-end —
    /// this proves it holds for a real Cashier vs. Admin JWT.
    /// </summary>
    public class SettingsHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SettingsHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private HttpClient CreateClient(string? role, string username = "settings.test.user")
        {
            var client = _factory.CreateClient();
            if (role != null)
            {
                var token = _factory.IssueToken(role, username);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        /// <summary>Sets a non-null SMTP host/backup path directly in the DB so the "omitted for Cashier" assertions can't pass by accident (default seed data leaves them null).</summary>
        private async Task SeedSensitiveFieldsAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var settings = await context.SystemSettings.FirstAsync(s => s.Id == 1);
            settings.SmtpHost = "smtp.example.com";
            settings.OffsiteBackupPath = "/mnt/backups";
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetSettings_NoToken_ReturnsUnauthorized()
        {
            var client = CreateClient(role: null);

            var response = await client.GetAsync("/api/settings");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSettings_CashierToken_OmitsSmtpAndBackupFields()
        {
            await SeedSensitiveFieldsAsync();
            var client = CreateClient(RoleNames.Cashier, "settings.cashier-get");

            var response = await client.GetAsync("/api/settings");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dto = await response.Content.ReadFromJsonAsync<SettingsDto>();
            Assert.NotNull(dto);
            Assert.Null(dto!.SmtpHost);
            Assert.Null(dto.OffsiteBackupPath);
        }

        [Fact]
        public async Task GetSettings_AdminToken_IncludesSmtpAndBackupFields()
        {
            await SeedSensitiveFieldsAsync();
            var client = CreateClient(RoleNames.Admin, "settings.admin-get");

            var response = await client.GetAsync("/api/settings");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var dto = await response.Content.ReadFromJsonAsync<SettingsDto>();
            Assert.NotNull(dto);
            Assert.Equal("smtp.example.com", dto!.SmtpHost);
            Assert.Equal("/mnt/backups", dto.OffsiteBackupPath);
        }

        [Fact]
        public async Task SaveSettings_CashierToken_ReturnsForbidden()
        {
            var client = CreateClient(RoleNames.Cashier, "settings.cashier-save");

            var response = await client.PutAsJsonAsync("/api/settings", new SaveSettingsDto { HotelName = "Should Not Save" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task SaveSettings_AdminToken_PersistsAndIsReflectedInSubsequentGet()
        {
            var client = CreateClient(RoleNames.Admin, "settings.admin-save");

            var saveResponse = await client.PutAsJsonAsync("/api/settings", new SaveSettingsDto
            {
                HotelName = "Sunrise Inn",
                HotelAddress = "12 Park Road",
                HotelGst = "27AAAAA0000A1Z5",
                ReceiptFormat = "Thermal"
            });
            Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

            var getResponse = await client.GetAsync("/api/settings");
            var dto = await getResponse.Content.ReadFromJsonAsync<SettingsDto>();
            Assert.Equal("Sunrise Inn", dto!.HotelName);
            Assert.Equal("12 Park Road", dto.HotelAddress);
        }

        [Fact]
        public async Task SaveSettings_OmittedSmtpPassword_PreservesExistingPassword()
        {
            var adminClient = CreateClient(RoleNames.Admin, "settings.password-preserve");

            // First save establishes an SMTP password.
            var firstSave = await adminClient.PutAsJsonAsync("/api/settings", new SaveSettingsDto
            {
                HotelName = "Preserve Test Hotel",
                SmtpHost = "smtp.example.com",
                SmtpPassword = "InitialSecret1!"
            });
            Assert.Equal(HttpStatusCode.NoContent, firstSave.StatusCode);

            // Second save omits SmtpPassword entirely — the controller must keep the existing one
            // rather than blanking it out.
            var secondSave = await adminClient.PutAsJsonAsync("/api/settings", new SaveSettingsDto
            {
                HotelName = "Preserve Test Hotel",
                SmtpHost = "smtp.example.com"
            });
            Assert.Equal(HttpStatusCode.NoContent, secondSave.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var settings = await context.SystemSettings.FirstAsync(s => s.Id == 1);
            Assert.Equal("InitialSecret1!", settings.SmtpPassword);
        }
    }
}
