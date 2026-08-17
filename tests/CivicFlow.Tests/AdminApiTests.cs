using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CivicFlow.Application.Auth;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class AdminApiTests
{
    private readonly HttpClient _client;

    public AdminApiTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Citizen_calling_admin_users_returns_403()
    {
        var token = await LoginAsync("citizen@civicflow.local");
        using var request = Authed(HttpMethod.Get, "/api/admin/users", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
