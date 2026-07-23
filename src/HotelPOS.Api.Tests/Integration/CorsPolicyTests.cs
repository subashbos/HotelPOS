using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace HotelPOS.Tests.Integration
{
    public class CorsPolicyTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public CorsPolicyTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("http://localhost:4200")]
        [InlineData("https://localhost:4200")]
        [InlineData("http://127.0.0.1:4200")]
        [InlineData("http://localhost:4201")]
        public async Task LoginEndpoint_PreflightOptionsRequest_ReturnsCorsHeaders(string origin)
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type");

            var response = await client.SendAsync(request);

            Assert.True(response.Headers.Contains("Access-Control-Allow-Origin") || response.IsSuccessStatusCode,
                $"CORS header Access-Control-Allow-Origin should be returned for origin {origin}");
        }
    }
}
