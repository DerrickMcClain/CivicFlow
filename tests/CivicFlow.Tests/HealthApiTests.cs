using System.Net;
using System.Net.Http.Json;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class HealthApiTests
{
    private readonly HttpClient _client;

    public HealthApiTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Health_returns_200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
    }
}
