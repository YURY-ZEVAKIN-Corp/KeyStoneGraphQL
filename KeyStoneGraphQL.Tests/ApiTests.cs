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

        [Fact]
        public async Task Post_GraphQL_HelloQuery_ReturnsHello()
        {
            // Arrange
            var query = new
            {
                query = "{ hello }"
            };
            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(query), System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/graphql", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Hello from HotChocolate GraphQL!", responseString);
        }
    }
}
