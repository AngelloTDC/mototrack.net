using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MotoTrack.Tests.Integration
{
    public class ApiMotoIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiMotoIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Motos_ReturnsOkAndJson()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/api/motos");

            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8",
                         response.Content.Headers.ContentType?.ToString());
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }
}
