using System.Net;
using System.Net.Http.Json;
using CivicFlow.Application.Auth;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class AuthApiTests
{
    private readonly HttpClient _client;

    public AuthApiTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Login_seeded_citizen_returns_token()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "citizen@civicflow.local",
            password = "CivicFlow!dev1"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Citizen", body!.Role);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
    }

    [Fact]
    public async Task Login_bad_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "citizen@civicflow.local",
            password = "wrong"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
