using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    /// <summary>
    /// Verifies the baseline security-headers middleware in Program.cs actually attaches its
    /// headers to real HTTP responses, including unauthenticated ones (the middleware sits ahead
    /// of authentication, so even a 401 must carry them).
    /// </summary>
    public class SecurityHeadersHttpTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public SecurityHeadersHttpTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AnyResponse_IncludesBaselineSecurityHeaders()
        {
            var client = _factory.CreateClient();

            using var response = await client.GetAsync("/api/audit"); // unauthenticated -> 401, headers must still be present

            Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions));
            Assert.Contains("nosniff", contentTypeOptions!);

            Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var frameOptions));
            Assert.Contains("DENY", frameOptions!);

            Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy));
            Assert.Contains("strict-origin-when-cross-origin", referrerPolicy!);
        }
    }
}
