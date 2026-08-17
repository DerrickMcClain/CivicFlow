using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CivicFlow.Application.Auth;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class CatalogApiTests
{
    private readonly HttpClient _client;

    public CatalogApiTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Request_types_include_residential_permit()
    {
        var token = await LoginAsync("citizen@civicflow.local");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/request-types");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var names = body.RootElement.EnumerateArray()
            .Select(x => x.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("Residential Permit", names);
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "CivicFlow!dev1"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }
}
