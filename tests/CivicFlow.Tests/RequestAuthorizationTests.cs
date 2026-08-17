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

    [Fact]
    public async Task Employee_illegal_transition_returns_409()
    {
        var citizenToken = await LoginAsync("citizen@civicflow.local");
        using var create = Authed(HttpMethod.Post, "/api/requests", citizenToken, new
        {
            requestTypeId = 1,
            title = "Driveway expansion",
            description = "Expand a residential driveway.",
            priority = 2
        });
        var created = await _client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        using var createdBody = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        var requestId = createdBody.RootElement.GetProperty("requestId").GetInt32();

        var employeeToken = await LoginAsync("employee@civicflow.local");
        using var status = Authed(HttpMethod.Put, $"/api/requests/{requestId}/status", employeeToken, new
        {
            status = "Approved",
            reason = "Skipping workflow"
        });
        var response = await _client.SendAsync(status);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Employee_queue_returns_200()
    {
        var token = await LoginAsync("employee@civicflow.local");
        using var request = Authed(HttpMethod.Get, "/api/employee/requests", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Employee_approve_returns_403()
    {
        var citizenToken = await LoginAsync("citizen@civicflow.local");
        var requestId = await CreateRequestAsync(citizenToken, "Patio cover");
        var employeeToken = await LoginAsync("employee@civicflow.local");

        using var approve = Authed(HttpMethod.Post, $"/api/requests/{requestId}/approve", employeeToken, new
        {
            reason = "Looks good"
        });
        var response = await _client.SendAsync(approve);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Supervisor_approve_happy_path_writes_history_and_audit()
    {
        var citizenToken = await LoginAsync("citizen@civicflow.local");
        var requestId = await CreateRequestAsync(citizenToken, "Shed permit");
        var employeeToken = await LoginAsync("employee@civicflow.local");

        await ChangeStatusAsync(employeeToken, requestId, "UnderReview");
        await ChangeStatusAsync(employeeToken, requestId, "EmployeeRecommendation");
        await ChangeStatusAsync(employeeToken, requestId, "SupervisorReview");

        var supervisorToken = await LoginAsync("supervisor@civicflow.local");
        using var approve = Authed(HttpMethod.Post, $"/api/requests/{requestId}/approve", supervisorToken, new
        {
            reason = "Meets ordinance"
        });
        var response = await _client.SendAsync(approve);

        response.EnsureSuccessStatusCode();
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Approved", body.RootElement.GetProperty("status").GetString());
        var history = body.RootElement.GetProperty("history").EnumerateArray().Select(x => x.GetProperty("newStatus").GetString()).ToList();
        Assert.Contains("SupervisorReview", history);
        Assert.Contains("Approved", history);
    }

    private async Task<int> CreateRequestAsync(string citizenToken, string title)
    {
        using var create = Authed(HttpMethod.Post, "/api/requests", citizenToken, new
        {
            requestTypeId = 1,
            title,
            description = $"{title} for a residential property.",
            priority = 2
        });
        var created = await _client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        using var createdBody = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
        return createdBody.RootElement.GetProperty("requestId").GetInt32();
    }

    private async Task ChangeStatusAsync(string token, int requestId, string status)
    {
        using var request = Authed(HttpMethod.Put, $"/api/requests/{requestId}/status", token, new
        {
            status,
            reason = $"Move to {status}"
        });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
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
