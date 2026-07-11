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

        // Generated per factory instance rather than a literal, so there's no
        // credential-shaped string for secret scanners to flag and no key reuse across runs.
        // AuthController derives its signing key via Encoding.UTF8.GetBytes(configuredKeyString),
        // so IssueToken below must do the same with this exact string to produce valid signatures.
        private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _connection.Open();

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = _signingKey,
                    ["Jwt:Issuer"] = TestJwtIssuer,
                    ["Jwt:Audience"] = TestJwtAudience,
                    ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:4200"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<HotelDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<HotelDbContext>(options => options.UseSqlite(_connection));
            });
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
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
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
