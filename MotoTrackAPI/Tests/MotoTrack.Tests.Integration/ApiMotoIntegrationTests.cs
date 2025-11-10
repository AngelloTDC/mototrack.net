using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MotoTrack.Tests.Integration
{
    public class ApiMotoIntegrationTests : IClassFixture<WebApplicationFactory<MotoTrackAPI.Program>>
    {
        private readonly WebApplicationFactory<MotoTrackAPI.Program> _factory;

        public ApiMotoIntegrationTests(WebApplicationFactory<MotoTrackAPI.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_Motos_ReturnsOkAndJson()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/motos");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200‑299
            Assert.Equal("application/json; charset=utf-8",
                         response.Content.Headers.ContentType.ToString());
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }
}
