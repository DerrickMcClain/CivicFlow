using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CivicFlow.Application.Auth;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class AuthApiTests
{
    private readonly HttpClient _client;
    private readonly CivicFlowApiFactory _factory;

    public AuthApiTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
        _factory = fixture.Factory;
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
    public async Task Me_returns_profile_for_local_token_when_entra_is_unconfigured()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "citizen@civicflow.local",
            password = "CivicFlow!dev1"
        });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Token;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("citizen@civicflow.local", body!.Email);
        Assert.Equal("Citizen", body.Role);
        Assert.Equal(string.Empty, body.Token);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    [Fact]
    public async Task Login_entra_provisioned_user_returns_401()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        if (!await db.Users.AnyAsync(x => x.Email == "entra-citizen@example.test"))
        {
            var entraUser = new User
            {
                FirstName = "Entra",
                LastName = "Citizen",
                Email = "entra-citizen@example.test",
                PasswordHash = EntraAuthConstants.PasswordSentinel,
                EntraObjectId = "11111111-1111-1111-1111-111111111111",
                RoleId = (int)RoleName.Citizen,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(entraUser);
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "entra-citizen@example.test",
            password = "CivicFlow!dev1"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
