using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CivicFlow.Application.Auth;
using CivicFlow.Domain.Entities;
using CivicFlow.Domain.Enums;
using CivicFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace CivicFlow.Tests;

[Collection("SqlServer")]
public class AdminEntraRoleTests
{
    private const string EntraRoleMessage = "Role is managed in Entra ID.";

    private readonly HttpClient _client;
    private readonly CivicFlowApiFactory _factory;

    public AdminEntraRoleTests(SqlServerFixture fixture)
    {
        _client = fixture.Client;
        _factory = fixture.Factory;
    }

    [Fact]
    public async Task Admin_cannot_change_role_of_entra_user()
    {
        var oid = Guid.NewGuid().ToString();
        int userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CivicFlowDbContext>();
            var entraUser = new User
            {
                FirstName = "Entra",
                LastName = "Staff",
                Email = $"entra-staff-{oid}@example.test",
                PasswordHash = EntraAuthConstants.PasswordSentinel,
                EntraObjectId = oid,
                RoleId = (int)RoleName.Supervisor,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };
            db.Users.Add(entraUser);
            await db.SaveChangesAsync();
            userId = entraUser.UserId;
        }

        var token = await LoginAsync("admin@civicflow.local");
        using var request = Authed(HttpMethod.Put, $"/api/admin/users/{userId}/role", token);
        request.Content = JsonContent.Create(new { role = "Employee" });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(403, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(EntraRoleMessage, document.RootElement.GetProperty("message").GetString());
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
