using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CivicFlow.Tests;

public sealed class CivicFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CivicFlowApiFactory(string connectionString)
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
    }
}
