using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelPOS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Hosts the real API pipeline (JWT auth + authorization + middleware) against an
    /// isolated in-memory SQLite database, so RBAC tests exercise the actual [Authorize]
    /// attributes instead of calling controller actions directly.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string TestJwtIssuer = "HotelPOS";
        public const string TestJwtAudience = "HotelPOSClient";

        // Program.cs reads and validates Jwt:Key eagerly (builder.Configuration.GetSection(...))
        // *before* builder.Build() runs, but WebApplicationFactory's ConfigureAppConfiguration
        // hook only applies at Build() time - too late for that check. The Jwt__Key env var
        // (double underscore = .NET's config-hierarchy separator) is picked up immediately by
        // WebApplication.CreateBuilder()'s default sources, with higher precedence than
        // appsettings.json's "" default, so it's visible in time - PROVIDED it's set before
        // CreateClient() first builds the host.
        //
        // Set once via an *explicit* static constructor rather than a field initializer: a type
        // with no explicit static constructor gets the `beforefieldinit` flag, which lets the
        // JIT defer field initializers until the field is first *read* - here, that's inside
        // IssueToken(), which every test calls *after* CreateClient() already triggered the host
        // build. An explicit static constructor removes beforefieldinit, giving a strict
        // guarantee it runs no later than this type's first instance construction (i.e. before
        // any test's CreateClient() call). Also shared (not per-instance) because it's a
        // process-wide env var and xUnit runs different test classes - and therefore different
        // CustomWebApplicationFactory instances - in parallel by default.
        private static readonly string SigningKey;

        static CustomWebApplicationFactory()
        {
            SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            Environment.SetEnvironmentVariable("Jwt__Key", SigningKey);
            Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        }

        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Removing only DbContextOptions<HotelDbContext> (and even DbContextOptions +
                // HotelDbContext alongside it) still left EF Core reporting both SqlServer and
                // Sqlite as registered. AddDbContext apparently registers the UseSqlServer(...)
                // configure-action itself as a separate, *accumulating* registration (options
                // pattern style) from the constructed DbContextOptions<HotelDbContext> instance -
                // so removing only the latter doesn't stop the former from also running against
                // the new builder. Cast the net over every descriptor whose service type mentions
                // HotelDbContext anywhere (closed generic arguments included), not just the
                // specific types AddDbContext is known to register.
                var descriptorsToRemove = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(HotelDbContext) ||
                    ContainsHotelDbContext(d.ServiceType)).ToList();
                foreach (var descriptor in descriptorsToRemove) services.Remove(descriptor);

                services.AddDbContext<HotelDbContext>(options => options.UseSqlite(_connection));
            });
        }

        private static bool ContainsHotelDbContext(Type type)
        {
            if (type == typeof(HotelDbContext)) return true;
            if (!type.IsGenericType) return false;
            return type.GetGenericArguments().Any(ContainsHotelDbContext);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            context.Database.EnsureCreated();

            return host;
        }

        /// <summary>Mints a JWT identical in shape to AuthController's, for a given role, without going through login.</summary>
        public string IssueToken(string role, string username = "test.user", int userId = 1)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: TestJwtIssuer,
                audience: TestJwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _connection.Dispose();
        }
    }
}
