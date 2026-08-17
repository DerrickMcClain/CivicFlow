using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CivicFlow.Tests;

/// <summary>
/// Host with both auth modes configured: the local seed JWT plus a stand-in Entra scheme.
/// AzureAd:SigningKey keeps the Entra scheme offline (no Authority / JWKS metadata call).
/// </summary>
public sealed class EntraApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public EntraApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:CivicFlow", _connectionString);
        builder.UseSetting("Jwt:Issuer", "CivicFlow");
        builder.UseSetting("Jwt:Audience", "CivicFlow");
        builder.UseSetting("Jwt:SigningKey", "DEV_ONLY_CHANGE_ME_32CHARS_MIN_KEY!!");
        builder.UseSetting("Jwt:ExpiryMinutes", "480");
        builder.UseSetting("AzureAd:TenantId", EntraTokenHelper.TenantId);
        builder.UseSetting("AzureAd:ClientId", EntraTokenHelper.ClientId);
        builder.UseSetting("AzureAd:Audience", EntraTokenHelper.Audience);
        builder.UseSetting("AzureAd:Issuer", EntraTokenHelper.Issuer);
        builder.UseSetting("AzureAd:SigningKey", EntraTokenHelper.SigningKey);
    }
}
