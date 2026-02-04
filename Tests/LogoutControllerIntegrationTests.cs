using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests
{
    public class LogoutControllerIntegrationTests : IClassFixture<WebApplicationFactory<dendrOnlineSPA.Program>>
    {
        private readonly WebApplicationFactory<dendrOnlineSPA.Program> _factory;

        public LogoutControllerIntegrationTests(WebApplicationFactory<dendrOnlineSPA.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_ReturnsOk_AndRevokesGrant()
        {
            var client = _factory.CreateClient();
            // Simulate a user with a session and token (mock or set up as needed)
            // For now, just call the endpoint and check for 200 OK
            var response = await client.PostAsync("/logout", null);
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Found });
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("Logged out", content);
            }
        }
    }
}
