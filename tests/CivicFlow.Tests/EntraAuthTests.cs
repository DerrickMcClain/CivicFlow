using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CivicFlow.Application.Auth;
using CivicFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicFlow.Tests;

[Collection("EntraSql")]
public class EntraAuthTests
{
    private const string MissingRoleMessage =
        "An Entra app role of Citizen, Employee, Supervisor, or Administrator is required.";

    private readonly HttpClient _client;
    private readonly EntraApiFactory _factory;

    public EntraAuthTests(EntraSqlFixture fixture)
    {
        _client = fixture.Client;
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Entra_citizen_token_me_returns_profile_and_persists_user()
    {
        var oid = Guid.NewGuid().ToString();
        var email = $"entra-{oid}@example.test";
        var token = EntraTokenHelper.CreateAccessToken("Citizen", oid, email, "Nora", "Newcomer");

        var response = await SendMeAsync(token);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Citizen", body!.Role);
        Assert.Equal(email, body.Email);
        Assert.Equal("Nora", body.FirstName);
        Assert.Equal("Newcomer", body.LastName);
        Assert.True(body.UserId > 0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        var persisted = await db.Users.Include(x => x.Role).SingleAsync(x => x.EntraObjectId == oid);
        Assert.Equal(body.UserId, persisted.UserId);
        Assert.Equal(EntraAuthConstants.PasswordSentinel, persisted.PasswordHash);
    }

    [Fact]
    public async Task Entra_token_without_roles_returns_403()
    {
        var oid = Guid.NewGuid().ToString();
        var token = EntraTokenHelper.CreateAccessToken(null, oid, $"noroles-{oid}@example.test");

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertErrorEnvelopeAsync(response, 403, MissingRoleMessage);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        Assert.False(await db.Users.AnyAsync(x => x.EntraObjectId == oid));
    }

    [Fact]
    public async Task Entra_token_unknown_role_returns_403()
    {
        var oid = Guid.NewGuid().ToString();
        var token = EntraTokenHelper.CreateAccessToken("NotARole", oid, $"unknown-{oid}@example.test");

        var response = await SendMeAsync(token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertErrorEnvelopeAsync(response, 403, MissingRoleMessage);
    }

    [Fact]
    public async Task Entra_second_request_same_oid_does_not_duplicate()
    {
        var oid = Guid.NewGuid().ToString();
        var email = $"repeat-{oid}@example.test";
        var token = EntraTokenHelper.CreateAccessToken("Employee", oid, email, "Pat", "Repeat");

        var first = await SendMeAsync(token);
        first.EnsureSuccessStatusCode();
        var second = await SendMeAsync(token);
        second.EnsureSuccessStatusCode();

        var firstBody = await first.Content.ReadFromJsonAsync<AuthResponse>();
        var secondBody = await second.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(firstBody!.UserId, secondBody!.UserId);
        Assert.Equal("Employee", secondBody.Role);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
        Assert.Equal(1, await db.Users.CountAsync(x => x.EntraObjectId == oid));
    }

    [Fact]
    public async Task Local_seed_login_still_works_on_entra_factory()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "citizen@civicflow.local",
            password = "CivicFlow!dev1"
        });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Token;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var me = await SendMeAsync(token);

        me.EnsureSuccessStatusCode();
        var body = await me.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("citizen@civicflow.local", body!.Email);
        Assert.Equal("Citizen", body.Role);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendMeAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private static async Task AssertErrorEnvelopeAsync(
        HttpResponseMessage response,
        int status,
        string message)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(message, document.RootElement.GetProperty("message").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
    }
}
