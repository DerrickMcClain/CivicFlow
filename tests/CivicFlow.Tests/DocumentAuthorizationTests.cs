using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CivicFlow.Application.Auth;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class DocumentAuthorizationTests
{
    private readonly HttpClient _client;

    public DocumentAuthorizationTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Citizen_can_upload_and_download_own_document()
    {
        var token = await LoginAsync("citizen@civicflow.local");
        var requestId = await CreateRequestAsync(token);

        using var upload = CreateUploadRequest(requestId, token, "evidence.txt", "text/plain", "Permit sketch notes");
        var uploadResponse = await _client.SendAsync(upload);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        using var uploadBody = await JsonDocument.ParseAsync(await uploadResponse.Content.ReadAsStreamAsync());
        var documentId = uploadBody.RootElement.GetProperty("documentId").GetInt32();

        using var detailRequest = Authed(HttpMethod.Get, $"/api/requests/{requestId}", token);
        var detailResponse = await _client.SendAsync(detailRequest);
        detailResponse.EnsureSuccessStatusCode();
        using var detailBody = await JsonDocument.ParseAsync(await detailResponse.Content.ReadAsStreamAsync());
        Assert.Equal(1, detailBody.RootElement.GetProperty("documents").GetArrayLength());

        using var downloadRequest = Authed(HttpMethod.Get, $"/api/requests/{requestId}/documents/{documentId}", token);
        var downloadResponse = await _client.SendAsync(downloadRequest);
        downloadResponse.EnsureSuccessStatusCode();
        var downloaded = await downloadResponse.Content.ReadAsStringAsync();
        Assert.Equal("Permit sketch notes", downloaded);
    }

    [Fact]
    public async Task Citizen_cannot_upload_to_another_citizens_request()
    {
        var ownerToken = await LoginAsync("citizen@civicflow.local");
        var requestId = await CreateRequestAsync(ownerToken);

        var outsider = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Other",
            lastName = "Citizen",
            email = $"other-{Guid.NewGuid():N}@civicflow.local",
            password = "CivicFlow!dev1"
        });
        outsider.EnsureSuccessStatusCode();
        var outsiderAuth = await outsider.Content.ReadFromJsonAsync<AuthResponse>();

        using var upload = CreateUploadRequest(requestId, outsiderAuth!.Token, "evidence.txt", "text/plain", "blocked");
        var response = await _client.SendAsync(upload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Citizen_cannot_download_internal_document()
    {
        var citizenToken = await LoginAsync("citizen@civicflow.local");
        var requestId = await CreateRequestAsync(citizenToken);
        var employeeToken = await LoginAsync("employee@civicflow.local");

        using var upload = CreateUploadRequest(requestId, employeeToken, "internal.txt", "text/plain", "staff only", isInternal: true);
        var uploadResponse = await _client.SendAsync(upload);
        uploadResponse.EnsureSuccessStatusCode();
        using var uploadBody = await JsonDocument.ParseAsync(await uploadResponse.Content.ReadAsStreamAsync());
        var documentId = uploadBody.RootElement.GetProperty("documentId").GetInt32();

        using var downloadRequest = Authed(HttpMethod.Get, $"/api/requests/{requestId}/documents/{documentId}", citizenToken);
        var response = await _client.SendAsync(downloadRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> CreateRequestAsync(string token)
    {
        using var create = Authed(HttpMethod.Post, "/api/requests", token, new
        {
            requestTypeId = 1,
            title = "Document test case",
            description = "Upload authorization coverage.",
            priority = 2
        });
        var response = await _client.SendAsync(create);
        response.EnsureSuccessStatusCode();
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return body.RootElement.GetProperty("requestId").GetInt32();
    }

    private static HttpRequestMessage CreateUploadRequest(
        int requestId,
        string token,
        string fileName,
        string contentType,
        string content,
        bool isInternal = false)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(isInternal.ToString().ToLowerInvariant()), "isInternal");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/requests/{requestId}/documents")
        {
            Content = form
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
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
