using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CivicFlow.Application.Auth;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class RequestAuthorizationTests
{
    private readonly HttpClient _client;

    public RequestAuthorizationTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Citizen_creates_request_201_and_audit()
    {
        var token = await LoginAsync("citizen@civicflow.local");
        using var request = Authed(HttpMethod.Post, "/api/requests", token, new
        {
            requestTypeId = 1,
            title = "Deck addition",
            description = "Need a permit for a backyard deck.",
            priority = 2
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var number = body.RootElement.GetProperty("requestNumber").GetString();
        Assert.StartsWith("CIV-", number);
    }

    [Fact]
    public async Task Citizen_A_cannot_read_citizen_B_request()
    {
        var ownerToken = await LoginAsync("citizen@civicflow.local");
        using var create = Authed(HttpMethod.Post, "/api/requests", ownerToken, new
        {
            requestTypeId = 1,
            title = "Fence replacement",
            description = "Replace an existing residential fence.",
            priority = 2
        });
        var created = await _client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        using var createdBody = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var requestId = createdBody.RootElement.GetProperty("requestId").GetInt32();

        var outsider = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Other",
            lastName = "Citizen",
            email = $"other-{Guid.NewGuid():N}@civicflow.local",
            password = "CivicFlow!dev1"
        });
        outsider.EnsureSuccessStatusCode();
        var outsiderAuth = await outsider.Content.ReadFromJsonAsync<AuthResponse>();

        using var read = Authed(HttpMethod.Get, $"/api/requests/{requestId}", outsiderAuth!.Token);
        var response = await _client.SendAsync(read);

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

    private static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
