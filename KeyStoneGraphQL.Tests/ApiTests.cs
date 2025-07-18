using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KeyStoneGraphQL.Tests
{
    public class ApiInitTests : IClassFixture<WebApplicationFactory<KeyStoneGraphQL.Program>>
    {
        private readonly HttpClient _client;

        public ApiInitTests(WebApplicationFactory<KeyStoneGraphQL.Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_Version_ReturnsSuccess()
        {
            // Act
            var response = await _client.GetAsync("/api/version");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
    }
}
